namespace UniversalToolchain.Benchmarks.Infrastructure;

public static class InputData
{
    public static readonly int X = 123;
    public static readonly int Y = 17;

    public static readonly int A = 5;
    public static readonly int B = 8;
    public static readonly int C = 11;
    public static readonly int D = 3;

    public static readonly double Price = 100.0;
    public static readonly double Fee = 5.0;

    public static readonly decimal DecimalPrice = 100.0m;
    public static readonly decimal DecimalFee = 5.0m;

    public static readonly int[] PredictableBranchInputs = [11, 12, 16, 30, 41, 9, 12, 33, 18, 15, 13, 7];
    public static readonly int[] RandomLikeBranchInputs = [5, -3, 9, 14, -11, 22, -1, 4, 17, 0, -8, 31];

    public static readonly (int a, int b, int c)[] PredictableShortCircuitInputs =
    [
        (3, 1, -9),
        (4, 2, -4),
        (1, 5, -7),
        (5, 2, -3),
        (2, 2, -1),
        (-5, 1, 7)
    ];

    public static readonly (int a, int b, int c)[] RandomLikeShortCircuitInputs =
    [
        (-3, -4, -1),
        (8, -2, -5),
        (-1, 6, 4),
        (9, 1, -6),
        (2, 4, -9),
        (-7, 5, 2)
    ];
}
