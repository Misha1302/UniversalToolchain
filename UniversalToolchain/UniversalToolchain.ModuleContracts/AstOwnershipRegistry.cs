namespace UniversalToolchain.ModuleContracts;

public sealed class AstOwnershipRegistry
{
    private readonly IReadOnlyList<AstOwnershipContract> _ownership;

    private AstOwnershipRegistry(IReadOnlyList<AstOwnershipContract> ownership)
    {
        _ownership = ownership;
    }

    public static AstOwnershipRegistry FromTable(SelectedModuleContractTable table)
    {
        table = table.ArgNotNull();

        var ownership = table.AstFacets
            .SelectMany(static x => x.AstOwnership)
            .OrderBy(static x => x.NodeKind.Value, StringComparer.Ordinal)
            .ThenBy(static x => x.OwnerModule.Value, StringComparer.Ordinal)
            .ToArray();

        return new AstOwnershipRegistry(ownership);
    }

    public IReadOnlyList<ToolchainDiagnostic> ValidateLowerer(IAstNodeLowerer lowerer)
    {
        lowerer = lowerer.ArgNotNull();

        var owners = GetOwners(lowerer.NodeKind);
        if (owners.Count == 0)
            return CreateZeroOwnerDiagnostic(lowerer.NodeKind);

        if (owners.Count > 1)
            return CreateMultipleOwnersDiagnostic(lowerer.NodeKind, owners);

        var owner = owners[0];
        if (owner.OwnerModule == lowerer.ModuleId && owner.Mode is AstOwnershipMode.Exclusive or AstOwnershipMode.Cooperative)
            return [];

        return
        [
            new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.LowererOwnershipMismatch,
                ToolchainDiagnosticSeverity.Warning,
                $"Lowerer '{lowerer.ModuleId}' handles AST node '{lowerer.NodeKind}', but ownership is declared by '{owner.OwnerModule}' as '{owner.Mode}'.",
                null,
                [new ToolchainDiagnosticHint("Declare matching AST ownership or keep the implementation as observer/validator-only.")])
        ];
    }

    public IReadOnlyList<ToolchainDiagnostic> ValidateNodeOwnership(AstNodeKind nodeKind)
    {
        var owners = GetOwners(nodeKind);
        return owners.Count switch
        {
            0 => CreateZeroOwnerDiagnostic(nodeKind),
            > 1 => CreateMultipleOwnersDiagnostic(nodeKind, owners),
            _ => []
        };
    }

    private IReadOnlyList<AstOwnershipContract> GetOwners(AstNodeKind nodeKind) =>
        _ownership
            .Where(x => x.NodeKind == nodeKind && x.Mode is AstOwnershipMode.Exclusive or AstOwnershipMode.Cooperative)
            .ToArray();

    private static IReadOnlyList<ToolchainDiagnostic> CreateZeroOwnerDiagnostic(AstNodeKind nodeKind) =>
    [
        new ToolchainDiagnostic(
            ModuleContractDiagnosticCodes.ZeroAstOwner,
            ToolchainDiagnosticSeverity.Warning,
            $"AST node '{nodeKind}' has no declared lowering owner.",
            null,
            [new ToolchainDiagnosticHint("Declare an AST ownership facet or keep the module explicitly Undeclared.")])
    ];

    private static IReadOnlyList<ToolchainDiagnostic> CreateMultipleOwnersDiagnostic(
        AstNodeKind nodeKind,
        IReadOnlyList<AstOwnershipContract> owners)
    {
        var ownerNames = string.Join(", ", owners.Select(static x => x.OwnerModule.Value));
        return
        [
            new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.MultipleAstOwners,
                ToolchainDiagnosticSeverity.Warning,
                $"AST node '{nodeKind}' has multiple declared lowering owners: {ownerNames}.",
                null,
                [new ToolchainDiagnosticHint("Use a single exclusive owner or declare an explicit cooperative lowering contract.")])
        ];
    }
}
