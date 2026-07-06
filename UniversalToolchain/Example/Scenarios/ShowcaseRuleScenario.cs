using UniversalToolchain.Wist;

namespace Example.Scenarios;

public static class ShowcaseRuleScenario
{
    private const string RolloutScoreFormula = "usage * 0.7 + reliability * 0.3 - incidents * 15.0";
    private const string DisallowedStatementRule = "let score = usage * 0.7\nscore";

    public static void Run(bool verbose)
    {
        using var rules = WistEngine.CreateSafeFormulas();

        Console.WriteLine("Tiny controlled rules for .NET");
        Console.WriteLine();
        Console.WriteLine("A product manager, config file, admin UI, or LLM suggests a numeric rollout rule:");
        Console.WriteLine(RolloutScoreFormula);
        Console.WriteLine();

        var rolloutScore = rules.Compile<Func<double, double, double, double>>(
            RolloutScoreFormula,
            "usage",
            "reliability",
            "incidents");

        var score = rolloutScore.CompiledDelegate(100.0d, 90.0d, 1.0d);
        var enableNewDashboard = score >= 80.0d;

        Console.WriteLine("Compiled once. Invoked as a typed .NET delegate.");
        Console.WriteLine("Input: usage=100, reliability=90, incidents=1");
        Console.WriteLine($"Score: {score}");
        Console.WriteLine($"Application decision: enableNewDashboard={enableNewDashboard}");
        Console.WriteLine();

        var accepted = rules.Validate(
            RolloutScoreFormula,
            new
            {
                usage = 100.0d,
                reliability = 90.0d,
                incidents = 1.0d
            });

        var rejected = rules.Validate(
            DisallowedStatementRule,
            new
            {
                usage = 100.0d,
                reliability = 90.0d,
                incidents = 1.0d
            });

        Console.WriteLine("The restricted formula profile validates before execution.");
        Console.WriteLine($"Allowed formula accepted: {accepted.IsValid}");
        Console.WriteLine($"Statement-style rule rejected: {!rejected.IsValid}");

        if (verbose)
        {
            Console.WriteLine();
            Console.WriteLine("Rejected rule:");
            Console.WriteLine(DisallowedStatementRule);
            Console.WriteLine();
            Console.WriteLine("Reason:");
            Console.WriteLine(rejected.Message);
        }
    }
}
