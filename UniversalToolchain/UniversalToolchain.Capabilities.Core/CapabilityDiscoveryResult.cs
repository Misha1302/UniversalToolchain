using System.Collections.ObjectModel;
using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class CapabilityDiscoveryResult
{
    private readonly ReadOnlyCollection<ToolchainDiagnostic> _diagnostics;
    private readonly ReadOnlyCollection<CapabilityProviderDescriptor> _providerDescriptors;

    public CapabilityDiscoveryResult(
        IEnumerable<CapabilityProviderDescriptor> providerDescriptors,
        IEnumerable<ToolchainDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(providerDescriptors);
        ArgumentNullException.ThrowIfNull(diagnostics);

        _providerDescriptors = new ReadOnlyCollection<CapabilityProviderDescriptor>(providerDescriptors.ToList());
        _diagnostics = new ReadOnlyCollection<ToolchainDiagnostic>(diagnostics.ToList());
    }

    public IReadOnlyList<CapabilityProviderDescriptor> ProviderDescriptors => _providerDescriptors;

    public IReadOnlyList<ToolchainDiagnostic> Diagnostics => _diagnostics;
}
