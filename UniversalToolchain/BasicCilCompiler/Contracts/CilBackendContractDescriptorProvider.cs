using UniversalToolchain.ModuleContracts;

namespace BasicCilCompiler.Contracts;

public sealed class CilBackendContractDescriptorProvider : IModuleContractDescriptorProvider
{
    private static readonly ContractNamespaceOwner CilNamespace = ContractNamespaceOwner.Reserved("cil-backend", "cil");

    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [ContractNamespaceOwner.Backend, CilNamespace];

    private readonly IReadOnlyList<IntrinsicSymbolId> _supportedIntrinsics;

    public CilBackendContractDescriptorProvider(IEnumerable<string>? supportedIntrinsics = null)
    {
        _supportedIntrinsics = Normalize(supportedIntrinsics ?? []);
    }

    public static ModuleId Module { get; } = new("backend.cil");

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new BackendCapabilityFacet(
            Module,
            [
                new BackendCapabilityContract(KnownCilBackendCapabilities.DynamicMethods, _supportedIntrinsics),
                new BackendCapabilityContract(KnownCilBackendCapabilities.NativeNumericIntrinsics, SelectNativeNumericIntrinsics()),
                new BackendCapabilityContract(KnownCilBackendCapabilities.NativeComparisonIntrinsics, SelectNativeComparisonIntrinsics()),
                new BackendCapabilityContract(KnownCilBackendCapabilities.LocalSlotOptimization, [])
            ])
    ];

    private static IReadOnlyList<IntrinsicSymbolId> Normalize(IEnumerable<string> supportedIntrinsics) =>
        supportedIntrinsics
            .Where(static intrinsic => !string.IsNullOrWhiteSpace(intrinsic))
            .Select(static intrinsic => new IntrinsicSymbolId(intrinsic))
            .OrderBy(static intrinsic => intrinsic.Value, StringComparer.Ordinal)
            .Distinct()
            .ToArray();

    private IReadOnlyList<IntrinsicSymbolId> SelectNativeNumericIntrinsics() =>
        _supportedIntrinsics
            .Where(static intrinsic =>
                intrinsic.Value.StartsWith("load_i", StringComparison.Ordinal) ||
                intrinsic.Value.StartsWith("load_f", StringComparison.Ordinal) ||
                intrinsic.Value == "load_decimal" ||
                intrinsic.Value.StartsWith("add_", StringComparison.Ordinal) ||
                intrinsic.Value.StartsWith("sub_", StringComparison.Ordinal) ||
                intrinsic.Value.StartsWith("mul_", StringComparison.Ordinal) ||
                intrinsic.Value.StartsWith("div_", StringComparison.Ordinal))
            .ToArray();

    private IReadOnlyList<IntrinsicSymbolId> SelectNativeComparisonIntrinsics() =>
        _supportedIntrinsics
            .Where(static intrinsic => intrinsic.Value.StartsWith("cmp_", StringComparison.Ordinal))
            .ToArray();
}
