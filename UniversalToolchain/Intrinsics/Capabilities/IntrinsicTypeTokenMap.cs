namespace BasicCore.Capabilities;

public static class IntrinsicTypeTokenMap
{
    public static bool TryResolveType(string token, out Type type)
    {
        type = token switch
        {
            "bool" => typeof(bool),
            "i32" => typeof(int),
            "i64" => typeof(long),
            "f32" => typeof(float),
            "f64" => typeof(double),
            "decimal" => typeof(decimal),
            _ => null!
        };

        return type != null;
    }

    public static bool TryResolveToken(Type type, out string token)
    {
        if (type == typeof(bool)) token = "bool";
        else if (type == typeof(int)) token = "i32";
        else if (type == typeof(long)) token = "i64";
        else if (type == typeof(float)) token = "f32";
        else if (type == typeof(double)) token = "f64";
        else if (type == typeof(decimal)) token = "decimal";
        else
        {
            token = string.Empty;
            return false;
        }

        return true;
    }
}
