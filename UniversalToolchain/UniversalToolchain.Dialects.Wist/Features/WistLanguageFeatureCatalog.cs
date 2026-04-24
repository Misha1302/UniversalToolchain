using UniversalToolchain.Features.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Features;

/// <summary>
///     Provides a data-only catalog describing the shipped Wist user-facing capabilities.
/// </summary>
public sealed class WistLanguageFeatureCatalog : ILanguageFeatureCatalog
{
    private static readonly IReadOnlyList<string> InterpreterAndCilBackends =
        [WistDialectBackendIds.Interpreter.Value, WistDialectBackendIds.Cil.Value];

    private readonly IReadOnlyList<LanguageFeatureDescriptor> _features;

    /// <summary>
    ///     Builds the deterministic Wist feature catalog.
    /// </summary>
    public WistLanguageFeatureCatalog()
    {
        _features = BuildFeatures()
            .OrderBy(static x => x.FeatureId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<LanguageFeatureDescriptor> GetFeatures()
    {
        return _features;
    }

    /// <inheritdoc />
    public bool TryGetFeature(
        LanguageFeatureId featureId,
        out LanguageFeatureDescriptor? descriptor)
    {
        descriptor = _features.FirstOrDefault(x => x.FeatureId == featureId);
        return descriptor is not null;
    }

    private static IReadOnlyList<LanguageFeatureDescriptor> BuildFeatures()
    {
        return
        [
            new(
                WistLanguageFeatureIds.ArithmeticExpressions,
                "Arithmetic expressions",
                LanguageFeatureKind.Syntax,
                ["Arithmetic"],
                [WistLanguageFeatureIds.StandardNumbers],
                [
                    new(
                        "+",
                        LanguageFeatureSymbolKind.Operator,
                        "left + right",
                        "Adds two numeric expressions."),
                    new(
                        "-",
                        LanguageFeatureSymbolKind.Operator,
                        "left - right",
                        "Subtracts one numeric expression from another."),
                    new(
                        "*",
                        LanguageFeatureSymbolKind.Operator,
                        "left * right",
                        "Multiplies numeric expressions."),
                    new(
                        "/",
                        LanguageFeatureSymbolKind.Operator,
                        "left / right",
                        "Divides numeric expressions.")
                ],
                InterpreterAndCilBackends,
                "Provides arithmetic operators over Wist numeric values."),

            new(
                WistLanguageFeatureIds.BooleanLogic,
                "Boolean logic",
                LanguageFeatureKind.Syntax,
                ["BooleanConditions", "Conditions"],
                [],
                [
                    new(
                        "and",
                        LanguageFeatureSymbolKind.Operator,
                        "left and right",
                        "Combines two boolean expressions."),
                    new(
                        "or",
                        LanguageFeatureSymbolKind.Operator,
                        "left or right",
                        "Evaluates logical disjunction."),
                    new(
                        "not",
                        LanguageFeatureSymbolKind.Operator,
                        "not value",
                        "Negates a boolean expression.")
                ],
                InterpreterAndCilBackends,
                "Provides boolean condition operators and boolean expression forms."),

            new(
                WistLanguageFeatureIds.CSharpInterop,
                "C# interop",
                LanguageFeatureKind.Interop,
                ["CSharpInterop"],
                [],
                [
                    new(
                        "csharp",
                        LanguageFeatureSymbolKind.HostBinding,
                        "csharp(...)",
                        "Binds selected trusted C# interop calls.")
                ],
                InterpreterAndCilBackends,
                "Allows trusted runtime access to selected C# interop forms."),

            new(
                WistLanguageFeatureIds.Comments,
                "Comments",
                LanguageFeatureKind.Syntax,
                ["Comments"],
                [],
                [
                    new(
                        "//",
                        LanguageFeatureSymbolKind.SyntaxForm,
                        "// comment",
                        "Starts a single-line comment.")
                ],
                InterpreterAndCilBackends,
                "Provides single-line comment syntax."),

            new(
                WistLanguageFeatureIds.ComparisonLogic,
                "Comparison logic",
                LanguageFeatureKind.Syntax,
                ["ComparisonConditions", "Conditions"],
                [WistLanguageFeatureIds.StandardNumbers],
                [
                    new(
                        "<",
                        LanguageFeatureSymbolKind.Operator,
                        "left < right",
                        "Checks whether the left value is less than the right value."),
                    new(
                        ">",
                        LanguageFeatureSymbolKind.Operator,
                        "left > right",
                        "Checks whether the left value is greater than the right value."),
                    new(
                        "<=",
                        LanguageFeatureSymbolKind.Operator,
                        "left <= right",
                        "Checks whether the left value is less than or equal to the right value."),
                    new(
                        ">=",
                        LanguageFeatureSymbolKind.Operator,
                        "left >= right",
                        "Checks whether the left value is greater than or equal to the right value.")
                ],
                InterpreterAndCilBackends,
                "Provides ordered comparison operators for Wist values."),

            new(
                WistLanguageFeatureIds.EqualityLogic,
                "Equality logic",
                LanguageFeatureKind.Syntax,
                ["Equality", "Conditions"],
                [],
                [
                    new(
                        "==",
                        LanguageFeatureSymbolKind.Operator,
                        "left == right",
                        "Checks whether two values are equal."),
                    new(
                        "!=",
                        LanguageFeatureSymbolKind.Operator,
                        "left != right",
                        "Checks whether two values are not equal.")
                ],
                InterpreterAndCilBackends,
                "Provides equality and inequality operators."),

            new(
                WistLanguageFeatureIds.Labels,
                "Labels",
                LanguageFeatureKind.Syntax,
                ["Labels"],
                [],
                [
                    new(
                        "label",
                        LanguageFeatureSymbolKind.SyntaxForm,
                        "label name:",
                        "Declares a jump target label.")
                ],
                InterpreterAndCilBackends,
                "Provides label declarations for control-flow oriented programs."),

            new(
                WistLanguageFeatureIds.Loops,
                "Loops",
                LanguageFeatureKind.Syntax,
                ["Loops"],
                [WistLanguageFeatureIds.Labels],
                [
                    new(
                        "goto",
                        LanguageFeatureSymbolKind.SyntaxForm,
                        "goto labelName",
                        "Transfers execution to a declared label.")
                ],
                InterpreterAndCilBackends,
                "Provides loop-oriented control flow based on labels and jumps."),

            new(
                WistLanguageFeatureIds.NativeNumbers,
                "Native numbers",
                LanguageFeatureKind.TypeSystem,
                ["NativeTypes"],
                [],
                [
                    new(
                        "number",
                        LanguageFeatureSymbolKind.Type,
                        "number",
                        "Native CLR numeric value support.")
                ],
                InterpreterAndCilBackends,
                "Provides native numeric values for typed execution profiles."),

            new(
                WistLanguageFeatureIds.SafeMathFunctions,
                "Safe math functions",
                LanguageFeatureKind.FunctionSet,
                ["NativeTypes", "SafeMathFunctions"],
                [],
                [
                    new(
                        "min",
                        LanguageFeatureSymbolKind.Function,
                        "min(number left, number right) -> number",
                        "Returns the smaller numeric value."),
                    new(
                        "max",
                        LanguageFeatureSymbolKind.Function,
                        "max(number left, number right) -> number",
                        "Returns the larger numeric value."),
                    new(
                        "abs",
                        LanguageFeatureSymbolKind.Function,
                        "abs(number value) -> number",
                        "Returns the absolute numeric value."),
                    new(
                        "clamp",
                        LanguageFeatureSymbolKind.Function,
                        "clamp(number value, number min, number max) -> number",
                        "Clamps a numeric value into an inclusive range.")
                ],
                InterpreterAndCilBackends,
                "Provides pure safe numeric helper functions for restricted formula and rule DSLs."),

            new(
                WistLanguageFeatureIds.Scopes,
                "Scopes",
                LanguageFeatureKind.Syntax,
                ["Scopes"],
                [],
                [
                    new(
                        "{ }",
                        LanguageFeatureSymbolKind.SyntaxForm,
                        "{ statements }",
                        "Groups statements into a lexical scope.")
                ],
                InterpreterAndCilBackends,
                "Provides nested statement scopes."),

            new(
                WistLanguageFeatureIds.SemicolonAsNewLine,
                "Semicolon as new line",
                LanguageFeatureKind.Syntax,
                ["SemicolonAsNewLine"],
                [],
                [
                    new(
                        ";",
                        LanguageFeatureSymbolKind.SyntaxForm,
                        "statement; nextStatement",
                        "Separates statements on a single line.")
                ],
                InterpreterAndCilBackends,
                "Allows semicolons to act as explicit statement separators."),

            new(
                WistLanguageFeatureIds.StandardNumbers,
                "Standard numbers",
                LanguageFeatureKind.TypeSystem,
                ["Numbers"],
                [],
                [
                    new(
                        "number",
                        LanguageFeatureSymbolKind.Type,
                        "number",
                        "Standard numeric literal support.")
                ],
                InterpreterAndCilBackends,
                "Provides standard numeric literals and numeric expression support."),

            new(
                WistLanguageFeatureIds.Variables,
                "Variables",
                LanguageFeatureKind.Syntax,
                ["Variables", "Identifier"],
                [WistLanguageFeatureIds.Scopes],
                [
                    new(
                        "let",
                        LanguageFeatureSymbolKind.SyntaxForm,
                        "let name = expression",
                        "Declares a scoped variable.")
                ],
                InterpreterAndCilBackends,
                "Provides variable declarations and variable reads.")
        ];
    }
}
