using UniversalToolchain.Capabilities.Abstractions;

namespace ConditionsModule;

public sealed class BooleanCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("BooleanConditions");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Boolean conditions",
                LanguageFeatureKind.Syntax,
                ["BooleanConditions"],
                [],
                [
                    new("true", LanguageFeatureSymbolKind.SyntaxForm, "true", "Represents the boolean true literal."),
                    new("false", LanguageFeatureSymbolKind.SyntaxForm, "false", "Represents the boolean false literal."),
                    new("and", LanguageFeatureSymbolKind.Operator, "bool and bool -> bool", "Combines two boolean expressions with conjunction."),
                    new("or", LanguageFeatureSymbolKind.Operator, "bool or bool -> bool", "Combines two boolean expressions with disjunction."),
                    new("not", LanguageFeatureSymbolKind.Operator, "not bool -> bool", "Negates a boolean expression.")
                ],
                ["cil", "interpreter"],
                "Provides boolean literals and boolean operators.")
        ];
    }
}
