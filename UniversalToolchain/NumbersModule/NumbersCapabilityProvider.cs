using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace NumbersModule;

public sealed class NumbersCapabilityProvider : ILanguageFeatureDescriptorProvider, IRuleRuntimeTypeBindingProvider
{
    private static readonly LanguageFeatureId FeatureId = new("NumericLiterals");
    private static readonly RuleTypeDescriptor NumberRuleType = new("number");
    private static readonly IRuleRuntimeValueConverter NumberConverter = new NumberRuleRuntimeValueConverter();

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Numeric literals",
                LanguageFeatureKind.Syntax,
                ["Numbers"],
                [],
                [
                    new("number", LanguageFeatureSymbolKind.SyntaxForm, "123 | 1.25 | 6.02e23", "Parses decimal and exponent-based numeric literals.")
                ],
                ["cil", "interpreter"],
                "Provides numeric literal parsing for Wist expressions.")
        ];
    }

    public IReadOnlyList<RuleRuntimeTypeBinding> GetRuleRuntimeTypeBindings()
    {
        return
        [
            new RuleRuntimeTypeBinding(NumberRuleType, typeof(RealNumberImpl), NumberConverter)
        ];
    }
}
