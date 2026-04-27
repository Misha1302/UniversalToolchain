using UniversalToolchain.Capabilities.Abstractions;

namespace CSharpInteropModule;

public sealed class CSharpInteropCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("CSharpInterop");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "C# interop",
                LanguageFeatureKind.Interop,
                ["CSharpInterop"],
                [],
                [
                    new("interop-call", LanguageFeatureSymbolKind.HostBinding, "Identifier(arg1, arg2, ...)", "Invokes a host-visible C# method through interop call syntax."),
                    new(",", LanguageFeatureSymbolKind.SyntaxForm, "arg1, arg2", "Separates interop call arguments.")
                ],
                ["cil", "interpreter"],
                "Provides host method invocation syntax for C# interop.")
        ];
    }
}
