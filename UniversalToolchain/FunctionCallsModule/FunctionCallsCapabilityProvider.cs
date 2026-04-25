namespace FunctionCallsModule;

public sealed class FunctionCallsCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("FunctionCalls");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Function calls",
                LanguageFeatureKind.Syntax,
                ["FunctionCalls"],
                [],
                [
                    new(
                        "function-call",
                        LanguageFeatureSymbolKind.SyntaxForm,
                        "name(argument, ...)",
                        "Calls a selected builtin function.")
                ],
                ["cil", "interpreter"],
                "Provides generic builtin function call syntax without declaring concrete functions.")
        ];
    }
}
