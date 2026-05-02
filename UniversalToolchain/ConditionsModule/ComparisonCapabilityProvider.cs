using UniversalToolchain.Capabilities.Abstractions;

namespace ConditionsModule;

public sealed class ComparisonCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("ComparisonConditions");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            FeatureId,
            "Comparison conditions",
            LanguageFeatureKind.Syntax,
            ["ComparisonConditions"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("==", LanguageFeatureSymbolKind.Operator, "value == value -> bool", "Compares two values for equality."),
                new LanguageFeatureSymbolDescriptor("!=", LanguageFeatureSymbolKind.Operator, "value != value -> bool", "Compares two values for inequality."),
                new LanguageFeatureSymbolDescriptor(">", LanguageFeatureSymbolKind.Operator, "value > value -> bool", "Checks whether the left value is greater than the right value."),
                new LanguageFeatureSymbolDescriptor("<", LanguageFeatureSymbolKind.Operator, "value < value -> bool", "Checks whether the left value is less than the right value."),
                new LanguageFeatureSymbolDescriptor(">=", LanguageFeatureSymbolKind.Operator, "value >= value -> bool", "Checks whether the left value is greater than or equal to the right value."),
                new LanguageFeatureSymbolDescriptor("<=", LanguageFeatureSymbolKind.Operator, "value <= value -> bool", "Checks whether the left value is less than or equal to the right value.")
            ],
            ["cil", "interpreter"],
            "Provides comparison operators for ordered and comparable values.")
    ];
}