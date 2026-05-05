using UniversalToolchain.Capabilities.Abstractions;

namespace ArithmeticModule;

public sealed class ArithmeticCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId _featureId = new("ArithmeticExpressions");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            _featureId,
            "Arithmetic expressions",
            LanguageFeatureKind.Syntax,
            ["Arithmetic"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("+", LanguageFeatureSymbolKind.Operator, "number + number -> number", "Adds two numeric values."),
                new LanguageFeatureSymbolDescriptor("-", LanguageFeatureSymbolKind.Operator, "number - number -> number", "Subtracts one numeric value from another."),
                new LanguageFeatureSymbolDescriptor("*", LanguageFeatureSymbolKind.Operator, "number * number -> number", "Multiplies two numeric values."),
                new LanguageFeatureSymbolDescriptor("/", LanguageFeatureSymbolKind.Operator, "number / number -> number", "Divides one numeric value by another."),
                new LanguageFeatureSymbolDescriptor("-", LanguageFeatureSymbolKind.Operator, "-number -> number", "Negates a numeric value.")
            ],
            ["cil", "interpreter"],
            "Provides arithmetic operators for numeric expressions.")
    ];
}