using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace ConditionsModule;

public sealed class BooleanCapabilityProvider : ILanguageFeatureDescriptorProvider, IRuleRuntimeTypeBindingProvider
{
    private static readonly LanguageFeatureId FeatureId = new("BooleanConditions");
    private static readonly RuleTypeDescriptor BoolRuleType = new("bool");
    private static readonly IRuleRuntimeValueConverter BoolConverter = new BooleanRuleRuntimeValueConverter();

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

    public IReadOnlyList<RuleRuntimeTypeBinding> GetRuleRuntimeTypeBindings()
    {
        return
        [
            new RuleRuntimeTypeBinding(BoolRuleType, typeof(bool), BoolConverter)
        ];
    }
}
