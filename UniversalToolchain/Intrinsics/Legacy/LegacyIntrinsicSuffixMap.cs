namespace BasicCore.Legacy;

/// <summary>
///     Provides exact suffix-to-runtime-type mapping for the legacy intrinsic naming scheme.
/// </summary>
public static class LegacyIntrinsicSuffixMap
{
    public static bool TryResolveType(string suffix, out Type type)
    {
        switch (suffix)
        {
            case "i32":
                type = typeof(int);
                return true;
            case "i64":
                type = typeof(long);
                return true;
            case "f32":
                type = typeof(float);
                return true;
            case "f64":
                type = typeof(double);
                return true;
            case "decimal":
                type = typeof(decimal);
                return true;
            default:
                type = null!;
                return false;
        }
    }

    public static bool TryResolveSuffix(Type type, out string suffix)
    {
        if (type == typeof(int))
        {
            suffix = "i32";
            return true;
        }

        if (type == typeof(long))
        {
            suffix = "i64";
            return true;
        }

        if (type == typeof(float))
        {
            suffix = "f32";
            return true;
        }

        if (type == typeof(double))
        {
            suffix = "f64";
            return true;
        }

        if (type == typeof(decimal))
        {
            suffix = "decimal";
            return true;
        }

        suffix = string.Empty;
        return false;
    }
}