using UniversalToolchain.ModuleContracts;

namespace BasicInterpreter.Contracts;

public sealed class InterpreterBackendContractDescriptorProvider : IModuleContractDescriptorProvider
{
    private static readonly ContractNamespaceOwner InterpreterNamespace = ContractNamespaceOwner.Reserved("interpreter-backend", "interpreter");

    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [ContractNamespaceOwner.Backend, InterpreterNamespace];

    private readonly IReadOnlyList<IntrinsicSymbolId> _supportedIntrinsics;

    public InterpreterBackendContractDescriptorProvider(IEnumerable<string>? supportedIntrinsics = null)
    {
        _supportedIntrinsics = (supportedIntrinsics ?? [])
            .Where(static intrinsic => !string.IsNullOrWhiteSpace(intrinsic))
            .Select(static intrinsic => new IntrinsicSymbolId(intrinsic))
            .OrderBy(static intrinsic => intrinsic.Value, StringComparer.Ordinal)
            .Distinct()
            .ToArray();
    }

    public static ModuleId Module { get; } = new("backend.interpreter");

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new BackendCapabilityFacet(
            Module,
            [
                new BackendCapabilityContract(KnownInterpreterBackendCapabilities.UniversalIntrinsicsOnly, _supportedIntrinsics),
                new BackendCapabilityContract(KnownInterpreterBackendCapabilities.NoNativeCilIntrinsics, [])
            ])
    ];
}
