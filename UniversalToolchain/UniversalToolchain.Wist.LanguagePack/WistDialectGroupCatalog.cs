namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistDialectGroupCatalog
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Groups { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["ArithmeticCore"] = ["Arithmetic", "Numbers", "Whitespaces"],
            ["ConditionsCore"] = ["BooleanConditions", "ComparisonConditions", "Conditions", "Equality"],
            ["VariablesCore"] = ["Identifier", "Variables"],
            ["BlocksCore"] = ["Scopes", "SemicolonAsNewLine"],
            ["ControlFlowCore"] = ["Loops", "Labels"]
        };
}
