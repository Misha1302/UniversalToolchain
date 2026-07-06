using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Fluent builder for deterministic runtime profile catalogs.
/// </summary>
public sealed class RuntimeProfileCatalogBuilder
{
    private readonly List<RuntimeProfileDefinition> _profiles = [];

    public static RuntimeProfileCatalogBuilder Create()
    {
        return new RuntimeProfileCatalogBuilder();
    }

    public RuntimeProfileCatalogBuilder Add(RuntimeProfileDefinition profile)
    {
        _profiles.Add(profile.NotNull());
        return this;
    }

    public RuntimeProfileCatalogBuilder Add(Func<RuntimeProfileCatalogBuilder, RuntimeProfileDefinitionBuilder> configure)
    {
        var builder = configure.ArgNotNull()(this).NotNull();
        return Add(builder.Build());
    }

    public RuntimeProfileCatalog Build()
    {
        return new RuntimeProfileCatalog(_profiles);
    }
}
