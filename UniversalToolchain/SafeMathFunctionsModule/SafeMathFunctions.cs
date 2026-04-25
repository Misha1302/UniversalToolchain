namespace SafeMathFunctionsModule;

public static class SafeMathFunctions
{
    public static double Abs(double value)
    {
        return Math.Abs(value);
    }

    public static double Clamp(double value, double min, double max)
    {
        return Math.Clamp(value, min, max);
    }

    public static double Max(double left, double right)
    {
        return Math.Max(left, right);
    }

    public static double Min(double left, double right)
    {
        return Math.Min(left, right);
    }

    public static double Round(double value)
    {
        return Math.Round(value);
    }
}
