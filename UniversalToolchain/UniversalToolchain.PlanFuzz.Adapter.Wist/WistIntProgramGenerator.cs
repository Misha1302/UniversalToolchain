namespace UniversalToolchain.PlanFuzz.Adapter.Wist;

/// <summary>
/// Generates bounded deterministic restricted-arithmetic programs and exposes known regressions only by explicit opt-in.
/// </summary>
public sealed class WistIntProgramGenerator
{
    private static readonly int[] InterestingValues = [-3, -2, -1, 0, 1, 2, 3];

    public WistIntProgramModel Generate(
        PlanFuzzRandom random,
        long caseIndex,
        bool includeRegressionCorpus)
    {
        random = random.ArgNotNull();
        if (includeRegressionCorpus && TryCreateRegressionCase(caseIndex, out var regression))
            return regression;

        return new WistIntProgramModel(
            GenerateExpression(random.Fork("expression"), depth: 3, forceParameter: random.NextBoolean()),
            InterestingValues[random.Fork("input").NextInt32(InterestingValues.Length)],
            "generated");
    }

    private static bool TryCreateRegressionCase(long caseIndex, out WistIntProgramModel model)
    {
        model = caseIndex switch
        {
            0 => new WistIntProgramModel(
                WistIntExpression.Multiply(WistIntExpression.Constant(0), WistIntExpression.Parameter()),
                7,
                "regression-corpus:issue-302"),
            1 => new WistIntProgramModel(
                WistIntExpression.Subtract(
                    WistIntExpression.Multiply(WistIntExpression.Constant(0), WistIntExpression.Constant(1)),
                    WistIntExpression.Constant(1)),
                0,
                "regression-corpus:issue-303"),
            2 => new WistIntProgramModel(
                WistIntExpression.Add(WistIntExpression.Parameter(), WistIntExpression.Constant(-2)),
                2,
                "regression-corpus:issue-307"),
            _ => null!
        };
        return model != null;
    }

    private static WistIntExpression GenerateExpression(
        PlanFuzzRandom random,
        int depth,
        bool forceParameter)
    {
        if (depth <= 0)
            return forceParameter || random.NextInt32(4) == 0
                ? WistIntExpression.Parameter()
                : WistIntExpression.Constant(InterestingValues[random.NextInt32(InterestingValues.Length)]);

        if (!forceParameter && random.NextInt32(5) == 0)
            return WistIntExpression.Constant(InterestingValues[random.NextInt32(InterestingValues.Length)]);

        var parameterOnLeft = forceParameter && random.NextBoolean();
        var left = GenerateExpression(random.Fork("left"), depth - 1, forceParameter && parameterOnLeft);
        var right = GenerateExpression(random.Fork("right"), depth - 1, forceParameter && !parameterOnLeft);
        return random.NextInt32(3) switch
        {
            0 => WistIntExpression.Add(left, right),
            1 => WistIntExpression.Subtract(left, right),
            _ => WistIntExpression.Multiply(left, right)
        };
    }
}
