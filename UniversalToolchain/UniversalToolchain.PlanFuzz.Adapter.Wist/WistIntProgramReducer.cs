namespace UniversalToolchain.PlanFuzz.Adapter.Wist;

internal static class WistIntProgramReducer
{
    public static long GetComplexity(WistIntProgramModel model)
    {
        model = model.ArgNotNull();
        return checked(ExpressionComplexity(model.Expression) * 1_000L + NumericComplexity(model.ParameterValue));
    }

    public static IReadOnlyList<PlanFuzzProgramReductionCandidate> CreateCandidates(
        PlanFuzzTestCase testCase,
        WistIntProgramModel model)
    {
        testCase = testCase.ArgNotNull();
        model = model.ArgNotNull();
        var currentComplexity = GetComplexity(model);
        var candidates = new List<PlanFuzzProgramReductionCandidate>();
        var seenPayloads = new HashSet<string>(StringComparer.Ordinal);

        void Add(string id, string summary, WistIntProgramModel candidate)
        {
            var complexity = GetComplexity(candidate);
            if (complexity >= currentComplexity)
                return;
            var program = CreateProgram(testCase, candidate);
            if (!seenPayloads.Add(program.Model.CanonicalJson))
                return;
            candidates.Add(new PlanFuzzProgramReductionCandidate(id, summary, complexity, program));
        }

        var expressionIndex = 0;
        foreach (var expression in ReduceExpression(model.Expression))
        {
            expressionIndex++;
            Add(
                $"expression-{expressionIndex.ToString("D4", CultureInfo.InvariantCulture)}",
                $"Reduce the structured Wist expression to '{expression.Render()}'.",
                model with { Expression = expression });
        }

        var parameterIndex = 0;
        foreach (var value in ReduceInteger(model.ParameterValue))
        {
            parameterIndex++;
            Add(
                $"parameter-{parameterIndex.ToString("D3", CultureInfo.InvariantCulture)}",
                $"Reduce external parameter x from {model.ParameterValue.ToString(CultureInfo.InvariantCulture)} to {value.ToString(CultureInfo.InvariantCulture)}.",
                model with { ParameterValue = value });
        }

        return new ReadOnlyCollection<PlanFuzzProgramReductionCandidate>(candidates
            .OrderBy(static candidate => candidate.Complexity)
            .ThenBy(static candidate => candidate.CandidateId, StringComparer.Ordinal)
            .ToArray());
    }

    private static IEnumerable<WistIntExpression> ReduceExpression(WistIntExpression expression)
    {
        expression = expression.ArgNotNull();
        if (expression.Kind == WistIntExpressionKind.Constant)
        {
            foreach (var value in ReduceInteger(expression.ConstantValue!.Value))
                yield return WistIntExpression.Constant(value);
            yield break;
        }

        if (expression.Kind == WistIntExpressionKind.Parameter)
            yield break;

        var left = expression.Left.NotNull();
        var right = expression.Right.NotNull();

        yield return left;
        yield return right;

        foreach (var reducedLeft in ReduceExpression(left))
            yield return Rebuild(expression.Kind, reducedLeft, right);
        foreach (var reducedRight in ReduceExpression(right))
            yield return Rebuild(expression.Kind, left, reducedRight);
    }

    private static IEnumerable<int> ReduceInteger(int value)
    {
        var candidates = new List<int>();
        void Add(int candidate)
        {
            if (candidate != value && !candidates.Contains(candidate) && NumericComplexity(candidate) < NumericComplexity(value))
                candidates.Add(candidate);
        }

        Add(0);
        Add(1);
        Add(-1);
        Add(value / 2);
        Add(Math.Sign(value));
        return candidates;
    }

    private static WistIntExpression Rebuild(
        WistIntExpressionKind kind,
        WistIntExpression left,
        WistIntExpression right) => kind switch
    {
        WistIntExpressionKind.Add => WistIntExpression.Add(left, right),
        WistIntExpressionKind.Subtract => WistIntExpression.Subtract(left, right),
        WistIntExpressionKind.Multiply => WistIntExpression.Multiply(left, right),
        _ => Thrower.NotSupported<WistIntExpression>($"Cannot rebuild non-binary Wist expression kind '{kind}'.")
    };

    private static PlanFuzzProgram CreateProgram(
        PlanFuzzTestCase testCase,
        WistIntProgramModel model) =>
        new(
            testCase.Program.ModelKind,
            testCase.Program.ModelSchemaVersion,
            model.ToPayload(),
            model.RenderSource(),
            testCase.Program.ProgramClass);

    private static long ExpressionComplexity(WistIntExpression expression)
    {
        expression = expression.ArgNotNull();
        return expression.Kind switch
        {
            WistIntExpressionKind.Constant => 1 + NumericComplexity(expression.ConstantValue!.Value),
            WistIntExpressionKind.Parameter => 1,
            WistIntExpressionKind.Add or WistIntExpressionKind.Subtract or WistIntExpressionKind.Multiply =>
                checked(1 + ExpressionComplexity(expression.Left.NotNull()) + ExpressionComplexity(expression.Right.NotNull())),
            _ => Thrower.NotSupported<long>($"Unsupported Wist expression kind '{expression.Kind}'.")
        };
    }

    private static long NumericComplexity(int value)
    {
        if (value == 0)
            return 0;
        if (value == 1)
            return 1;
        if (value == -1)
            return 2;
        return 10L + value.ToString(CultureInfo.InvariantCulture).Length;
    }
}
