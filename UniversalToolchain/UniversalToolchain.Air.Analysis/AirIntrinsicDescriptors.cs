using System.Collections.ObjectModel;
using System.Reflection;
using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.Air.Analysis;

public sealed class AirIntrinsicDescriptor
{
    public AirIntrinsicDescriptor(
        string id,
        IEnumerable<AirValueTypeId>? parameterTypes = null,
        IEnumerable<AirValueTypeId>? resultTypes = null,
        int dataOperandCount = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("AIR intrinsic identifier must not be empty.", nameof(id));
        if (dataOperandCount < 0)
            throw new ArgumentOutOfRangeException(nameof(dataOperandCount), dataOperandCount, "Data operand count must not be negative.");

        Id = id.Trim();
        ParameterTypes = new ReadOnlyCollection<AirValueTypeId>((parameterTypes ?? []).ToList());
        ResultTypes = new ReadOnlyCollection<AirValueTypeId>((resultTypes ?? []).ToList());
        DataOperandCount = dataOperandCount;
    }

    public string Id { get; }

    public IReadOnlyList<AirValueTypeId> ParameterTypes { get; }

    public IReadOnlyList<AirValueTypeId> ResultTypes { get; }

    public int DataOperandCount { get; }
}

public interface IAirIntrinsicDescriptorResolver
{
    bool TryResolve(Instruction instruction, out AirIntrinsicDescriptor descriptor, out string? diagnostic);
}

public sealed class AirIntrinsicDescriptorSet : IAirIntrinsicDescriptorResolver
{
    private readonly ReadOnlyCollection<AirIntrinsicDescriptor> _descriptors;
    private readonly Dictionary<string, AirIntrinsicDescriptor> _byId;

    public AirIntrinsicDescriptorSet(IEnumerable<AirIntrinsicDescriptor>? descriptors = null)
    {
        var ordered = (descriptors ?? [])
            .OrderBy(static x => x.Id, StringComparer.Ordinal)
            .ToList();

        _byId = new Dictionary<string, AirIntrinsicDescriptor>(StringComparer.Ordinal);
        foreach (var descriptor in ordered)
        {
            if (!_byId.TryAdd(descriptor.Id, descriptor))
                throw new ArgumentException($"Duplicate AIR intrinsic descriptor '{descriptor.Id}'.", nameof(descriptors));
        }

        _descriptors = new ReadOnlyCollection<AirIntrinsicDescriptor>(ordered);
    }

    public static AirIntrinsicDescriptorSet Empty { get; } = new();

    public IReadOnlyList<AirIntrinsicDescriptor> Values => _descriptors;

    public bool TryGet(string id, out AirIntrinsicDescriptor descriptor) =>
        _byId.TryGetValue(id, out descriptor!);

    public bool TryResolve(Instruction instruction, out AirIntrinsicDescriptor descriptor, out string? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        descriptor = default!;
        diagnostic = null;

        if (instruction.Operands.Count == 0 || instruction.Operands[0] is not string intrinsicId)
        {
            diagnostic = "AIR Intrinsic must start with a string intrinsic identifier.";
            return false;
        }

        if (TryGet(intrinsicId, out descriptor))
            return true;

        diagnostic = $"Unsupported AIR intrinsic '{intrinsicId}'.";
        return false;
    }
}

public sealed class AirIntrinsicDescriptorResolverSet : IAirIntrinsicDescriptorResolver
{
    private readonly ReadOnlyCollection<IAirIntrinsicDescriptorResolver> _resolvers;

    public AirIntrinsicDescriptorResolverSet(IEnumerable<IAirIntrinsicDescriptorResolver>? resolvers = null)
    {
        _resolvers = new ReadOnlyCollection<IAirIntrinsicDescriptorResolver>((resolvers ?? []).ToList());
    }

    public static AirIntrinsicDescriptorResolverSet Empty { get; } = new();

    public bool TryResolve(Instruction instruction, out AirIntrinsicDescriptor descriptor, out string? diagnostic)
    {
        descriptor = default!;
        diagnostic = null;

        foreach (var resolver in _resolvers)
        {
            if (resolver.TryResolve(instruction, out descriptor, out var candidateDiagnostic))
                return true;

            diagnostic ??= candidateDiagnostic;
        }

        diagnostic ??= "AIR intrinsic is not supported by the configured descriptor resolvers.";
        return false;
    }
}

public static class AirIntrinsicIds
{
    public const string CallCSharp = "call C#";

    public const string CallCSharpConstructor = "call C# ctor";

    public const string AddInt32Unchecked = "add_i32";

    public const string SubtractInt32Unchecked = "sub_i32";

    public const string MultiplyInt32Unchecked = "mul_i32";

    public const string EqualInt32 = "cmp_eq_i32";
}

public sealed class AirManagedCallIntrinsicDescriptorResolver : IAirIntrinsicDescriptorResolver
{
    public static AirManagedCallIntrinsicDescriptorResolver Instance { get; } = new();

    public bool TryResolve(Instruction instruction, out AirIntrinsicDescriptor descriptor, out string? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        descriptor = default!;
        diagnostic = null;

        if (instruction.Operands.Count == 0 || instruction.Operands[0] is not string intrinsicId)
        {
            diagnostic = "AIR Intrinsic must start with a string intrinsic identifier.";
            return false;
        }

        if (intrinsicId == AirIntrinsicIds.CallCSharp)
            return TryResolveMethod(instruction, out descriptor, out diagnostic);

        if (intrinsicId == AirIntrinsicIds.CallCSharpConstructor)
            return TryResolveConstructor(instruction, out descriptor, out diagnostic);

        return false;
    }

    private static bool TryResolveMethod(Instruction instruction, out AirIntrinsicDescriptor descriptor, out string? diagnostic)
    {
        descriptor = default!;
        diagnostic = null;

        if (instruction.Operands.Count != 2)
        {
            diagnostic = $"AIR intrinsic '{AirIntrinsicIds.CallCSharp}' expects one data operand.";
            return false;
        }

        if (!TryExtractMethod(instruction.Operands[1], out var method, out var consumesInstanceReceiver, out diagnostic))
            return false;

        if (!TryCreateParameterTypes(method, consumesInstanceReceiver, out var parameterTypes, out diagnostic))
            return false;

        if (!TryCreateResultTypes(method.ReturnType, out var resultTypes, out diagnostic))
            return false;

        descriptor = new AirIntrinsicDescriptor(
            AirIntrinsicIds.CallCSharp,
            parameterTypes,
            resultTypes,
            dataOperandCount: 1);
        return true;
    }

    private static bool TryResolveConstructor(Instruction instruction, out AirIntrinsicDescriptor descriptor, out string? diagnostic)
    {
        descriptor = default!;
        diagnostic = null;

        if (instruction.Operands.Count != 2)
        {
            diagnostic = $"AIR intrinsic '{AirIntrinsicIds.CallCSharpConstructor}' expects one data operand.";
            return false;
        }

        if (instruction.Operands[1] is not ConstructorInfo ctor)
        {
            diagnostic = $"AIR intrinsic '{AirIntrinsicIds.CallCSharpConstructor}' requires a ConstructorInfo data operand.";
            return false;
        }

        var parameterTypes = new List<AirValueTypeId>();
        foreach (var parameter in ctor.GetParameters())
        {
            if (!TryMapClrType(parameter.ParameterType, out var parameterType))
            {
                diagnostic = $"Constructor '{ctor}' parameter '{parameter.Name}' has unsupported CLR type '{parameter.ParameterType}'.";
                return false;
            }

            parameterTypes.Add(parameterType);
        }

        descriptor = new AirIntrinsicDescriptor(
            AirIntrinsicIds.CallCSharpConstructor,
            parameterTypes,
            [AirValueTypes.Object],
            dataOperandCount: 1);
        return true;
    }

    private static bool TryExtractMethod(
        object? operand,
        out MethodInfo method,
        out bool consumesInstanceReceiver,
        out string? diagnostic)
    {
        method = default!;
        consumesInstanceReceiver = false;
        diagnostic = null;

        if (operand is MethodInfo methodInfo)
        {
            method = methodInfo;
            consumesInstanceReceiver = !methodInfo.IsStatic;
            return true;
        }

        var operandType = operand?.GetType();
        var methodProperty = operandType?.GetProperty("Method", BindingFlags.Public | BindingFlags.Instance);
        if (methodProperty?.GetValue(operand) is not MethodInfo descriptorMethod)
        {
            diagnostic = $"AIR intrinsic '{AirIntrinsicIds.CallCSharp}' requires a MethodInfo or descriptor with MethodInfo Method property.";
            return false;
        }

        var receiverProperty = operandType!.GetProperty("Receiver", BindingFlags.Public | BindingFlags.Instance);
        var receiver = receiverProperty?.GetValue(operand);
        var receiverTypeName = receiver?.GetType().FullName ?? string.Empty;
        if (receiverTypeName.EndsWith("+Static", StringComparison.Ordinal) ||
            receiverTypeName.EndsWith(".Static", StringComparison.Ordinal))
        {
            method = descriptorMethod;
            consumesInstanceReceiver = !descriptorMethod.IsStatic;
            return true;
        }

        diagnostic = $"AIR intrinsic '{AirIntrinsicIds.CallCSharp}' supports MethodInfo and static C# call descriptors; execution-scoped provider descriptors are runtime-bound and are not representable in backend-neutral stack analysis yet.";
        return false;
    }

    private static bool TryCreateParameterTypes(
        MethodInfo method,
        bool consumesInstanceReceiver,
        out IReadOnlyList<AirValueTypeId> parameterTypes,
        out string? diagnostic)
    {
        var types = new List<AirValueTypeId>();
        parameterTypes = types;
        diagnostic = null;

        if (method.ContainsGenericParameters)
        {
            diagnostic = $"Method '{method}' has unresolved generic parameters.";
            return false;
        }

        if (consumesInstanceReceiver)
            types.Add(AirValueTypes.Object);

        foreach (var parameter in method.GetParameters())
        {
            if (!TryMapClrType(parameter.ParameterType, out var parameterType))
            {
                diagnostic = $"Method '{method}' parameter '{parameter.Name}' has unsupported CLR type '{parameter.ParameterType}'.";
                return false;
            }

            types.Add(parameterType);
        }

        return true;
    }

    private static bool TryCreateResultTypes(
        Type returnType,
        out IReadOnlyList<AirValueTypeId> resultTypes,
        out string? diagnostic)
    {
        diagnostic = null;
        if (returnType == typeof(void))
        {
            resultTypes = [];
            return true;
        }

        if (!TryMapClrType(returnType, out var resultType))
        {
            resultTypes = [];
            diagnostic = $"Method return type '{returnType}' is not supported by AIR stack analysis.";
            return false;
        }

        resultTypes = [resultType];
        return true;
    }

    private static bool TryMapClrType(Type type, out AirValueTypeId airType)
    {
        if (type == typeof(bool))
        {
            airType = AirValueTypes.Bool;
            return true;
        }

        if (type == typeof(int))
        {
            airType = AirValueTypes.Int32;
            return true;
        }

        if (type == typeof(double))
        {
            airType = AirValueTypes.Float64;
            return true;
        }

        if (!type.IsByRef && !type.IsPointer && !type.IsValueType)
        {
            airType = AirValueTypes.Object;
            return true;
        }

        airType = default;
        return false;
    }
}

public static class AirCoreIntrinsicDescriptors
{
    public static AirIntrinsicDescriptorSet ArithmeticInt32 { get; } = new(
    [
        new AirIntrinsicDescriptor(
            AirIntrinsicIds.AddInt32Unchecked,
            [AirValueTypes.Int32, AirValueTypes.Int32],
            [AirValueTypes.Int32]),
        new AirIntrinsicDescriptor(
            AirIntrinsicIds.SubtractInt32Unchecked,
            [AirValueTypes.Int32, AirValueTypes.Int32],
            [AirValueTypes.Int32]),
        new AirIntrinsicDescriptor(
            AirIntrinsicIds.MultiplyInt32Unchecked,
            [AirValueTypes.Int32, AirValueTypes.Int32],
            [AirValueTypes.Int32]),
        new AirIntrinsicDescriptor(
            AirIntrinsicIds.EqualInt32,
            [AirValueTypes.Int32, AirValueTypes.Int32],
            [AirValueTypes.Bool])
    ]);

    public static IAirIntrinsicDescriptorResolver DefaultResolver =>
        new AirIntrinsicDescriptorResolverSet(
        [
            AirManagedCallIntrinsicDescriptorResolver.Instance,
            ArithmeticInt32
        ]);
}
