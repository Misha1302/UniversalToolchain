namespace NativeMathModule;

public static class NativeArithmetic
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [UsedImplicitly]
    public static T Add<T>(T a, T b) where T : INumber<T> => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [UsedImplicitly]
    public static T Subtract<T>(T a, T b) where T : INumber<T> => a - b;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [UsedImplicitly]
    public static T Multiply<T>(T a, T b) where T : INumber<T> => a * b;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [UsedImplicitly]
    public static T Divide<T>(T a, T b) where T : INumber<T> => a / b;


    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    [UsedImplicitly]
    public static decimal AddDecimal(decimal a, decimal b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    [UsedImplicitly]
    public static decimal SubtractDecimal(decimal a, decimal b) => a - b;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    [UsedImplicitly]
    public static decimal MultiplyDecimal(decimal a, decimal b) => a * b;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    [UsedImplicitly]
    public static decimal DivideDecimal(decimal a, decimal b) => a / b;
}