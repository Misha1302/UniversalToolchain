namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeComponentCatalogProvider
{
    IReadOnlyList<RuntimeComponentDescriptor> GetComponents();
}