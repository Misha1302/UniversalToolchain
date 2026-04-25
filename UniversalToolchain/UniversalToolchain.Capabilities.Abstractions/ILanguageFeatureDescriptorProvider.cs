namespace UniversalToolchain.Capabilities.Abstractions;

public interface ILanguageFeatureDescriptorProvider
{
    IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures();
}
