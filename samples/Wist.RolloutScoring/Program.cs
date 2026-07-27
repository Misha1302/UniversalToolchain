using UniversalToolchain.Wist;

const string formula = "usage * 0.7 + reliability * 0.3 - incidents * 15.0";
const string unsupportedStatement = "let score = usage * 0.7\nscore";

using var rules = WistEngine.CreateRestrictedArithmetic();

var inputs = new
{
    usage = 100.0,
    reliability = 90.0,
    incidents = 1.0
};

var validation = rules.Validate(formula, inputs);
if (!validation.IsValid)
    throw new InvalidOperationException(
        $"The documented rollout formula must validate: {string.Join("; ", validation.Diagnostics.Select(static diagnostic => diagnostic.Message))}");

var rolloutScore = rules.Compile<Func<double, double, double, double>>(
    formula,
    "usage",
    "reliability",
    "incidents");

var score = rolloutScore.CompiledDelegate(
    inputs.usage,
    inputs.reliability,
    inputs.incidents);

var rejected = rules.Validate(unsupportedStatement, inputs);
if (rejected.IsValid)
    throw new InvalidOperationException("The restricted preset unexpectedly accepted a statement-style binding.");

Console.WriteLine("Wist restricted formula demo");
Console.WriteLine();
Console.WriteLine($"formula: {formula}");
Console.WriteLine("✓ validated before execution");
Console.WriteLine("✓ compiled once to Func<double, double, double, double>");
Console.WriteLine($"✓ score: {score:F1} -> enable dashboard: {score >= 80.0}");
Console.WriteLine("✗ rejected a statement-style rule before execution");
Console.WriteLine();
Console.WriteLine("The formula returns data. The host application owns the action.");
