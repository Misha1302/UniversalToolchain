using BasicCore.Contracts;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Classifies selected runtime modules by activation role and validates activation compatibility.
/// </summary>
public sealed class SelectedRuntimeModuleClassifier
{
    private readonly IRuntimeComponentTypeLoader _typeLoader;

    public SelectedRuntimeModuleClassifier(IRuntimeComponentTypeLoader typeLoader)
    {
        _typeLoader = typeLoader.ArgNotNull();
    }

    public SelectedRuntimeModuleClassification Classify(IEnumerable<RuntimeComponentManifestEntry> modules)
    {
        modules = modules.ArgNotNull();

        var frontendModuleTypes = new List<Type>();
        var irModuleTypes = new List<Type>();

        foreach (var entry in modules)
        {
            var moduleEntry = entry.ArgNotNull();
            var type = LoadModuleType(moduleEntry);
            var isFrontendModule = typeof(IFrontendCoreModule).IsAssignableFrom(type);
            var isIrModule = typeof(IIRProcessingModule).IsAssignableFrom(type);

            if (isFrontendModule)
                frontendModuleTypes.Add(type);

            if (isIrModule)
                irModuleTypes.Add(type);

            if (!isFrontendModule && !isIrModule)
                Thrower.InvalidOpEx(
                    $"Runtime module '{moduleEntry.CanonicalAlias}' resolves to type '{DisplayName(type)}', but the type does not implement IFrontendCoreModule or IIRProcessingModule.");
        }

        return new SelectedRuntimeModuleClassification(frontendModuleTypes, irModuleTypes);
    }

    private Type LoadModuleType(RuntimeComponentManifestEntry entry)
    {
        if (entry.Kind != RuntimeComponentKind.FrontendModule)
            Thrower.InvalidOpEx(
                $"Runtime component '{entry.CanonicalAlias}' has kind '{RuntimeComponentKindCodec.Format(entry.Kind)}', but '{RuntimeComponentKindCodec.Format(RuntimeComponentKind.FrontendModule)}' was expected.");

        return _typeLoader.LoadType(entry);
    }

    private static string DisplayName(Type type) => type.FullName ?? type.Name;
}