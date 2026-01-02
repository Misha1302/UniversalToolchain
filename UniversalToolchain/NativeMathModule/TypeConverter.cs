namespace NativeMathModule;

public static class TypeConverter
{
    public static int ToInt(object value)
    {
        return Convert.ToInt32(value);
    }

    public static long ToLong(object value)
    {
        return Convert.ToInt64(value);
    }

    public static float ToSingle(object value)
    {
        return Convert.ToSingle(value);
    }

    public static double ToDouble(object value)
    {
        return Convert.ToDouble(value);
    }

    public static decimal ToDecimal(object value)
    {
        return Convert.ToDecimal(value);
    }

    // Синонимы для удобства
    public static float ToFloat(object value)
    {
        return ToSingle(value);
    }
}