using UniversalToolchain.Capabilities.Abstractions;

namespace CSharpInteropModule;

public sealed class CSharpInteropCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId _featureId = new("CSharpInterop");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            _featureId,
            "C# interop",
            LanguageFeatureKind.Interop,
            ["CSharpInterop"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("interop-call", LanguageFeatureSymbolKind.HostBinding, "Identifier(arg1, arg2, ...)", "Invokes a host-visible C# method through interop call syntax."),
                new LanguageFeatureSymbolDescriptor(",", LanguageFeatureSymbolKind.SyntaxForm, "arg1, arg2", "Separates interop call arguments.")
            ],
            ["cil", "interpreter"],
            "Provides host method invocation syntax for C# interop.")
    ];
}