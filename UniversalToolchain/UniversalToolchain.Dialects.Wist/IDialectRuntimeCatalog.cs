using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public interface IDialectRuntimeCatalog
{
    bool TryResolveModule(string alias, out DialectRuntimeModuleDescriptor? descriptor);

    bool TryResolveOptimizer(string alias, out DialectRuntimeOptimizerDescriptor? descriptor);

    bool TryResolveBackend(DialectBackendId id, out DialectRuntimeBackendDescriptor? descriptor);

    IReadOnlyCollection<DialectRuntimeModuleDescriptor> Modules { get; }

    IReadOnlyCollection<DialectRuntimeOptimizerDescriptor> Optimizers { get; }

    IReadOnlyCollection<DialectRuntimeBackendDescriptor> Backends { get; }
}
