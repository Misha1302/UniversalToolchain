namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Contributes runtime descriptors through a framework-native extension seam.
/// </summary>
public interface IDialectRuntimeDescriptorProvider
{
    decimal Order { get; }

    void Register(DialectRuntimeDescriptorRegistryBuilder builder);
}