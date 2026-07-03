namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public sealed class GeneratedSemanticParityTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [TestCaseSource(nameof(ArithmeticParityCases))]
    public void CompilerAndInterpreter_ShouldStayAligned_ForGeneratedArithmeticExpressions(string expression, int depth, int caseIndex)
    {
        using var helper = new ModulePipelineTestHelper();

        var result = helper.ExecuteBoth(expression, _modules);

        Assert.That(caseIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(depth, Is.GreaterThanOrEqualTo(0));
        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
    }

    [TestCaseSource(nameof(NeutralTransformationCases))]
    public void NeutralArithmeticTransformations_ShouldPreserveGeneratedExpressionValue(string expression, int depth, int caseIndex)
    {
        using var helper = new ModulePipelineTestHelper();

        Assert.That(caseIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(depth, Is.GreaterThanOrEqualTo(0));
        helper.ExecuteEquivalent(expression, $"(({expression}) + 0) * 1", _modules);
    }

    private static IEnumerable<TestCaseData> ArithmeticParityCases() =>
        GenerateArithmeticExpressionCases(maxDepth: 4)
            .Take(64)
            .Select(static item => new TestCaseData(item.Expression, item.Depth, item.CaseIndex)
                .SetName($"ArithmeticParity_depth{item.Depth}_case{item.CaseIndex:000}_{SanitizeCaseName(item.Expression)}"));

    private static IEnumerable<TestCaseData> NeutralTransformationCases() =>
        GenerateArithmeticExpressionCases(maxDepth: 3)
            .Take(32)
            .Select(static item => new TestCaseData(item.Expression, item.Depth, item.CaseIndex)
                .SetName($"NeutralArithmetic_depth{item.Depth}_case{item.CaseIndex:000}_{SanitizeCaseName(item.Expression)}"));

    private static IEnumerable<(string Expression, int Depth, int CaseIndex)> GenerateArithmeticExpressionCases(int maxDepth) =>
        GenerateArithmeticExpressions(maxDepth)
            .Select(static (item, index) => (item.Expression, item.Depth, CaseIndex: index));

    private static IEnumerable<(string Expression, int Depth)> GenerateArithmeticExpressions(int maxDepth)
    {
        var atoms = new[] { "0", "1", "2", "3", "5", "8", "-1", "-3" };
        foreach (var atom in atoms)
            yield return (atom, 0);

        var previousDepth = atoms.AsEnumerable();
        for (var depth = 1; depth <= maxDepth; depth++)
        {
            var nextDepth = new List<string>();
            foreach (var left in previousDepth)
            {
                foreach (var right in atoms.Take(4))
                {
                    nextDepth.Add($"({left} + {right})");
                    nextDepth.Add($"({left} - {right})");
                    nextDepth.Add($"({left} * {right})");
                }
            }

            foreach (var expression in nextDepth)
                yield return (expression, depth);

            previousDepth = nextDepth;
        }
    }

    private static string SanitizeCaseName(string expression)
    {
        var safe = new string(expression
            .Select(static ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray());
        while (safe.Contains("__", StringComparison.Ordinal))
            safe = safe.Replace("__", "_", StringComparison.Ordinal);

        return safe.Trim('_');
    }
}
