namespace RuleDeclarationsModule;

public sealed class RuleDeclarationsCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("RuleDeclarations");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Rule declarations",
                LanguageFeatureKind.RuleModel,
                ["RuleDeclarations"],
                [],
                [
                    new(
                        "rule",
                        LanguageFeatureSymbolKind.RuleForm,
                        "rule Name(param: type) -> type { expression }",
                        "Declares a named, typed host-facing rule."),
                    new(
                        "typed-rule-parameters",
                        LanguageFeatureSymbolKind.RuleForm,
                        "parameter: type",
                        "Declares rule input names and their host-facing types.")
                ],
                ["cil", "interpreter"],
                "Provides host-facing rule declaration metadata and enables RuleSet compilation above the canonical Wist runtime path.")
        ];
    }
}
