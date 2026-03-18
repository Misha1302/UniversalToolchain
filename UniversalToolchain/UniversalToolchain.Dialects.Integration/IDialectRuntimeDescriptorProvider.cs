namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Contributes runtime descriptors through a framework-native extension seam.
/// </summary>
public interface IDialectRuntimeDescriptorProvider
{
    int Order { get; }

    void Register(DialectRuntimeDescriptorRegistryBuilder builder);
}
