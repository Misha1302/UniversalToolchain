using UniversalToolchain.Capabilities.Abstractions;

namespace ConditionsModule;

public sealed class BooleanCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId _featureId = new("BooleanConditions");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            _featureId,
            "Boolean conditions",
            LanguageFeatureKind.Syntax,
            ["BooleanConditions"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("true", LanguageFeatureSymbolKind.SyntaxForm, "true", "Represents the boolean true literal."),
                new LanguageFeatureSymbolDescriptor("false", LanguageFeatureSymbolKind.SyntaxForm, "false", "Represents the boolean false literal."),
                new LanguageFeatureSymbolDescriptor("and", LanguageFeatureSymbolKind.Operator, "bool and bool -> bool", "Combines two boolean expressions with conjunction."),
                new LanguageFeatureSymbolDescriptor("or", LanguageFeatureSymbolKind.Operator, "bool or bool -> bool", "Combines two boolean expressions with disjunction."),
                new LanguageFeatureSymbolDescriptor("not", LanguageFeatureSymbolKind.Operator, "not bool -> bool", "Negates a boolean expression.")
            ],
            ["cil", "interpreter"],
            "Provides boolean literals and boolean operators.")
    ];
}