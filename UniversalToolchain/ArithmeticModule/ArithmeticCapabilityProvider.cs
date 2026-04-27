using UniversalToolchain.Capabilities.Abstractions;

namespace ArithmeticModule;

public sealed class ArithmeticCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("ArithmeticExpressions");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Arithmetic expressions",
                LanguageFeatureKind.Syntax,
                ["Arithmetic"],
                [],
                [
                    new("+", LanguageFeatureSymbolKind.Operator, "number + number -> number", "Adds two numeric values."),
                    new("-", LanguageFeatureSymbolKind.Operator, "number - number -> number", "Subtracts one numeric value from another."),
                    new("*", LanguageFeatureSymbolKind.Operator, "number * number -> number", "Multiplies two numeric values."),
                    new("/", LanguageFeatureSymbolKind.Operator, "number / number -> number", "Divides one numeric value by another."),
                    new("-", LanguageFeatureSymbolKind.Operator, "-number -> number", "Negates a numeric value.")
                ],
                ["cil", "interpreter"],
                "Provides arithmetic operators for numeric expressions.")
        ];
    }
}
