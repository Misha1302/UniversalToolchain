using System.Reflection;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Normalizes implementation-owned runtime values before they cross a public
/// Wist boundary. The check is intentionally exact and does not scan arbitrary
/// members or infer numeric identities from unrelated user types.
/// </summary>
internal static class WistRuntimeValueNormalizer
{
    private const string RealNumberAssemblyName = "NumbersModule";
    private const string RealNumberTypeName = "NumbersModule.Core.RealNumberImpl";

    public static object? Normalize(object? value)
    {
        if (value == null)
            return null;

        var type = value.GetType();
        if (!string.Equals(type.Assembly.GetName().Name, RealNumberAssemblyName, StringComparison.Ordinal) ||
            !string.Equals(type.FullName, RealNumberTypeName, StringComparison.Ordinal))
        {
            return value;
        }

        var getValue = type.GetMethod(
            "GetValue",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        if (getValue == null || getValue.ReturnType != typeof(double))
        {
            throw new InvalidOperationException(
                $"Runtime value '{RealNumberTypeName}' does not satisfy its stable boundary contract.");
        }

        return (double)getValue.Invoke(value, null)!;
    }
}
