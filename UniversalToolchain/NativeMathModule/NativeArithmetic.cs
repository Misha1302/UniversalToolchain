using System.Numerics;

namespace NativeMathModule;

public static class NativeArithmetic
{
    public static T Add<T>(T a, T b) where T : INumber<T>
    {
        return a + b;
    }

    public static T Subtract<T>(T a, T b) where T : INumber<T>
    {
        return a - b;
    }

    public static T Multiply<T>(T a, T b) where T : INumber<T>
    {
        return a * b;
    }

    public static T Divide<T>(T a, T b) where T : INumber<T>
    {
        return a / b;
    }
}