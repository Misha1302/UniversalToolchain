using System.Reflection;
using System.Text;
using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

internal sealed record SsaManagedCallableSemantics(
    SemanticEffectSummary Effects,
    Determinism Determinism,
    AlgebraicTraits AlgebraicTraits,
    SemanticTrustLevel TrustLevel);

public static class SsaManagedCallables
{
    private const string MethodPrefix = "ssa.managed.method.v1:";
    private const string ConstructorPrefix = "ssa.managed.ctor.v1:";

    public static bool IsManagedCallable(CallableId id) =>
        id.Value.StartsWith(MethodPrefix, StringComparison.Ordinal) ||
        id.Value.StartsWith(ConstructorPrefix, StringComparison.Ordinal);

    public static bool TryCreateMethod(
        MethodInfo method,
        bool consumesInstanceReceiver,
        out CallableId callable,
        out CallableDescriptor descriptor,
        out string? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(method);

        callable = default;
        descriptor = default!;
        diagnostic = null;

        if (!ValidateMethod(method, consumesInstanceReceiver, out diagnostic))
            return false;

        var parameters = method.GetParameters().Select(static x => x.ParameterType).ToArray();
        callable = new CallableId(
            MethodPrefix +
            string.Join(
                ":",
                Encode(TypeName(method.DeclaringType!)),
                Encode(method.Name),
                consumesInstanceReceiver ? "1" : "0",
                Encode(TypeName(method.ReturnType)),
                EncodeTypeList(parameters)));

        return TryCreateMethodDescriptor(callable, method, consumesInstanceReceiver, out descriptor, out diagnostic);
    }

    public static bool TryCreateConstructor(
        ConstructorInfo constructor,
        out CallableId callable,
        out CallableDescriptor descriptor,
        out string? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(constructor);

        callable = default;
        descriptor = default!;
        diagnostic = null;

        if (constructor.DeclaringType is null)
        {
            diagnostic = $"Constructor '{constructor}' has no declaring type.";
            return false;
        }

        var parameters = constructor.GetParameters().Select(static x => x.ParameterType).ToArray();
        callable = new CallableId(
            ConstructorPrefix +
            string.Join(
                ":",
                Encode(TypeName(constructor.DeclaringType)),
                EncodeTypeList(parameters)));

        return TryCreateConstructorDescriptor(callable, constructor, out descriptor, out diagnostic);
    }

    private static bool TryCreateMethodDescriptor(
        CallableId callable,
        MethodInfo method,
        bool consumesInstanceReceiver,
        out CallableDescriptor descriptor,
        out string? diagnostic)
    {
        descriptor = default!;
        diagnostic = null;

        if (!ValidateMethod(method, consumesInstanceReceiver, out diagnostic))
            return false;

        var parameterTypes = new List<SemanticTypeId>();
        if (consumesInstanceReceiver)
            parameterTypes.Add(SsaSemanticTypes.Object);

        foreach (var parameter in method.GetParameters())
        {
            if (!TryMapClrType(parameter.ParameterType, out var type))
            {
                diagnostic = $"Method '{method}' parameter '{parameter.Name}' has unsupported CLR type '{parameter.ParameterType}'.";
                return false;
            }

            parameterTypes.Add(new SemanticTypeId(type.Value));
        }

        var resultTypes = new List<SemanticTypeId>();
        if (method.ReturnType != typeof(void))
        {
            if (!TryMapClrType(method.ReturnType, out var type))
            {
                diagnostic = $"Method '{method}' return type '{method.ReturnType}' is unsupported.";
                return false;
            }

            resultTypes.Add(new SemanticTypeId(type.Value));
        }

        var semantics = ResolveMethodSemantics(method);
        descriptor = new CallableDescriptor(
            callable,
            new CallableSignature(parameterTypes, resultTypes),
            effects: semantics.Effects,
            determinism: semantics.Determinism,
            algebraicTraits: semantics.AlgebraicTraits,
            trustLevel: semantics.TrustLevel,
            displayName: $"managed method {method.DeclaringType!.FullName}.{method.Name}");
        return true;
    }

    private static bool TryCreateConstructorDescriptor(
        CallableId callable,
        ConstructorInfo constructor,
        out CallableDescriptor descriptor,
        out string? diagnostic)
    {
        descriptor = default!;
        diagnostic = null;

        var parameterTypes = new List<SemanticTypeId>();
        foreach (var parameter in constructor.GetParameters())
        {
            if (!TryMapClrType(parameter.ParameterType, out var type))
            {
                diagnostic = $"Constructor '{constructor}' parameter '{parameter.Name}' has unsupported CLR type '{parameter.ParameterType}'.";
                return false;
            }

            parameterTypes.Add(new SemanticTypeId(type.Value));
        }

        var semantics = ResolveConstructorSemantics(constructor);
        descriptor = new CallableDescriptor(
            callable,
            new CallableSignature(parameterTypes, [SsaSemanticTypes.Object]),
            effects: semantics.Effects,
            determinism: semantics.Determinism,
            algebraicTraits: semantics.AlgebraicTraits,
            trustLevel: semantics.TrustLevel,
            displayName: $"managed constructor {constructor.DeclaringType!.FullName}");
        return true;
    }

    private static SsaManagedCallableSemantics ResolveMethodSemantics(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<SsaManagedCallableAttribute>(inherit: false);
        if (attribute is not null)
            return FromAttribute(attribute);

        return new SsaManagedCallableSemantics(
            new SemanticEffectSummary(
            [
                SemanticEffectKind.CallsExternalCode,
                SemanticEffectKind.MayThrow,
                SemanticEffectKind.UnknownExternalEffect
            ]),
            Determinism.Unknown,
            AlgebraicTraits.None,
            SemanticTrustLevel.ExternalUnknown);
    }

    private static SsaManagedCallableSemantics ResolveConstructorSemantics(ConstructorInfo constructor)
    {
        var attribute = constructor.GetCustomAttribute<SsaManagedCallableAttribute>(inherit: false);
        if (attribute is not null)
            return FromAttribute(attribute);

        return new SsaManagedCallableSemantics(
            new SemanticEffectSummary(
            [
                SemanticEffectKind.Allocates,
                SemanticEffectKind.CallsExternalCode,
                SemanticEffectKind.MayThrow
            ]),
            Determinism.Unknown,
            AlgebraicTraits.None,
            SemanticTrustLevel.ExternalUnknown);
    }

    private static SsaManagedCallableSemantics FromAttribute(SsaManagedCallableAttribute attribute)
    {
        if (attribute.IsPure)
        {
            return new SsaManagedCallableSemantics(
                SemanticEffectSummary.Pure,
                attribute.Determinism,
                attribute.AlgebraicTraits,
                attribute.TrustLevel);
        }

        var effects = new List<SemanticEffectKind>();
        if (attribute.ReadsRuntimeState)
            effects.Add(SemanticEffectKind.ReadsRuntimeState);
        if (attribute.WritesRuntimeState)
            effects.Add(SemanticEffectKind.WritesRuntimeState);
        if (attribute.ReadsMemory)
            effects.Add(SemanticEffectKind.ReadsMemory);
        if (attribute.WritesMemory)
            effects.Add(SemanticEffectKind.WritesMemory);
        if (attribute.Allocates)
            effects.Add(SemanticEffectKind.Allocates);
        if (attribute.MayThrow)
            effects.Add(SemanticEffectKind.MayThrow);
        if (attribute.CallsExternalCode)
            effects.Add(SemanticEffectKind.CallsExternalCode);
        if (attribute.UnknownExternalEffect)
            effects.Add(SemanticEffectKind.UnknownExternalEffect);

        return new SsaManagedCallableSemantics(
            new SemanticEffectSummary(effects),
            attribute.Determinism,
            attribute.AlgebraicTraits,
            attribute.TrustLevel);
    }

    private static bool ValidateMethod(MethodInfo method, bool consumesInstanceReceiver, out string? diagnostic)
    {
        diagnostic = null;

        if (method.DeclaringType is null)
        {
            diagnostic = $"Method '{method}' has no declaring type.";
            return false;
        }

        if (method.ContainsGenericParameters)
        {
            diagnostic = $"Method '{method}' has unresolved generic parameters.";
            return false;
        }

        if (!method.IsStatic && !consumesInstanceReceiver)
        {
            diagnostic = $"Instance method '{method}' requires a stack receiver; execution-scoped provider calls are not representable as backend-neutral SSA managed callables yet.";
            return false;
        }

        return true;
    }

    private static bool TryMapClrType(Type type, out SsaTypeId ssaType)
    {
        if (type == typeof(bool))
        {
            ssaType = SsaTypes.Bool;
            return true;
        }

        if (type == typeof(int))
        {
            ssaType = SsaTypes.Int32;
            return true;
        }

        if (type == typeof(double))
        {
            ssaType = SsaTypes.Float64;
            return true;
        }

        if (!type.IsByRef && !type.IsPointer && !type.IsValueType)
        {
            ssaType = SsaTypes.Object;
            return true;
        }

        ssaType = default;
        return false;
    }

    private static string TypeName(Type type) =>
        type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    private static string EncodeTypeList(IEnumerable<Type> types) =>
        string.Join(";", types.Select(static type => Encode(TypeName(type))));

    private static string Encode(string value)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        return encoded.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

}
