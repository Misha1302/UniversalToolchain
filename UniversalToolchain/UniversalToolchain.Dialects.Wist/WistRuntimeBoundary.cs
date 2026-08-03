using ExceptionsManager;
using System.Globalization;
using System.Reflection;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Owns the stable CLR boundary for one composed Wist runtime. Presets backed by
/// NumbersModule use an implementation-owned numeric value internally, while
/// callers continue to provide and receive ordinary CLR numerics.
/// </summary>
internal sealed class WistRuntimeBoundary
{
    private const string RealNumberAssemblyName = "NumbersModule";
    private const string RealNumberTypeName = "NumbersModule.Core.RealNumberImpl";

    private readonly Type? _realNumberType;
    private readonly ConstructorInfo? _realNumberConstructor;

    private WistRuntimeBoundary(Type? realNumberType)
    {
        _realNumberType = realNumberType;
        if (realNumberType == null)
            return;

        _realNumberConstructor = realNumberType.GetConstructor([typeof(double)])
            ?? throw new InvalidOperationException(
                $"Runtime numeric type '{RealNumberTypeName}' does not expose the required public double constructor.");
    }

    public static WistRuntimeBoundary Create(ToolchainRuntimeConfiguration configuration)
    {
        configuration = configuration.ArgNotNull();
        var numbersAssembly = configuration.RequiredInfrastructureModules
            .Concat(configuration.FrontendModules)
            .Concat(configuration.IrModules)
            .Concat(configuration.Optimizers)
            .Select(static type => type.Assembly)
            .FirstOrDefault(static assembly => string.Equals(
                assembly.GetName().Name,
                RealNumberAssemblyName,
                StringComparison.Ordinal));

        if (numbersAssembly == null)
            return new WistRuntimeBoundary(null);

        var realNumberType = numbersAssembly.GetType(RealNumberTypeName, throwOnError: false, ignoreCase: false)
            ?? throw new InvalidOperationException(
                $"Selected runtime assembly '{RealNumberAssemblyName}' does not contain '{RealNumberTypeName}'.");
        return new WistRuntimeBoundary(realNumberType);
    }

    public Type NormalizeDeclaredType(Type publicType)
    {
        publicType = publicType.ArgNotNull();
        return _realNumberType != null && (IsClrNumericType(publicType) || IsRealNumberType(publicType))
            ? _realNumberType
            : publicType;
    }

    public object? NormalizeArgument(object? value)
    {
        if (value == null || _realNumberType == null || _realNumberType.IsInstanceOfType(value))
            return value;

        var valueType = value.GetType();
        double numericValue;
        if (IsClrNumericType(valueType))
        {
            numericValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        else if (IsRealNumberType(valueType))
        {
            var getValue = valueType.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes)
                ?? throw new InvalidOperationException(
                    $"Host numeric value '{valueType.AssemblyQualifiedName}' does not expose the required GetValue() method.");
            numericValue = Convert.ToDouble(getValue.Invoke(value, null), CultureInfo.InvariantCulture);
        }
        else
        {
            return value;
        }

        return _realNumberConstructor!.Invoke([numericValue]);
    }

    public IReadOnlyDictionary<string, object?> NormalizeArguments(
        IReadOnlyDictionary<string, object?> arguments)
    {
        arguments = arguments.ArgNotNull();
        var normalized = new Dictionary<string, object?>(arguments.Count, StringComparer.Ordinal);
        foreach (var argument in arguments)
            normalized.Add(argument.Key, NormalizeArgument(argument.Value));
        return normalized;
    }

    private static bool IsRealNumberType(Type type) =>
        string.Equals(type.FullName, RealNumberTypeName, StringComparison.Ordinal) &&
        string.Equals(type.Assembly.GetName().Name, RealNumberAssemblyName, StringComparison.Ordinal);

    private static bool IsClrNumericType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return Type.GetTypeCode(type) is
            TypeCode.SByte or TypeCode.Byte or
            TypeCode.Int16 or TypeCode.UInt16 or
            TypeCode.Int32 or TypeCode.UInt32 or
            TypeCode.Int64 or TypeCode.UInt64 or
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
    }
}
