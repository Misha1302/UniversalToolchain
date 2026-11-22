namespace UniversalToolchain.ModuleContracts;

/// <summary>
///     Resolves who may lower an AST node kind, and reports the three ways a contract can disagree about it.
/// </summary>
/// <remarks>
///     <para>
///         <c>UT-MOD-OWNERSHIP-003</c> used to compare the lowerer against a single <c>OwnerModule</c> and ignore
///         <see cref="AstOwnershipContract.CooperatingModules" /> entirely. That field was declared, validated for
///         null, copied into every runtime snapshot - and read by nothing. The consequence was that cooperative
///         lowering was impossible by construction: a contract that declared <c>Cooperative</c> and named a helper
///         still reported the helper as an intruder. The documented remedy for an ownership conflict ("declare an
///         explicit cooperative lowering contract") could not be followed.
///     </para>
///     <para>
///         <c>UT-MOD-OWNERSHIP-002</c> had the matching half of the same bug: it counted every ownership-bearing
///         declaration, so two modules that declared <em>cooperative</em> lowering of one node - the exact shape
///         cooperation is for - were reported as conflicting.
///     </para>
///     <para>
///         Both are now one resolution rather than two filters. A node has at most one exclusive owner; cooperative
///         owners form one group with their named helpers; exclusive and cooperative declarations for the same node
///         contradict each other, because an exclusive owner cannot also share.
///     </para>
/// </remarks>
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

        var resolution = Resolve(lowerer.NodeKind);
        return resolution.Outcome switch
        {
            OwnershipOutcome.Unowned => CreateZeroOwnerDiagnostic(lowerer.NodeKind),
            OwnershipOutcome.Competing => CreateMultipleOwnersDiagnostic(lowerer.NodeKind, resolution.CompetingOwners),
            _ when resolution.LoweringModules.Contains(lowerer.ModuleId) => [],
            _ => CreateLowererMismatchDiagnostic(lowerer.NodeKind, lowerer.ModuleId, resolution.LoweringModules)
        };
    }

    /// <summary>
    ///     Reports every node kind whose ownership declarations contradict each other across the whole table.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the table-wide entry point, called by <see cref="ModuleContractTableBuilder.Build" />. It reports
    ///         only conflicts, not zero owners, because those are different claims: a conflict is a contradiction
    ///         between two declarations that are both present, while an absent declaration may be a node the frontend
    ///         produces and never lowers. Reporting the second one from the table build would be a guess about a node
    ///         the table cannot see the fate of.
    ///     </para>
    ///     <para>
    ///         Until this call existed the registry had no production caller at all: all three codes were documented as
    ///         live while nothing in the pipeline could emit them, which is the same defect that kept
    ///         <c>UT-CONTRACT-002</c> invisible until the table build called its validator.
    ///     </para>
    /// </remarks>
    public static IReadOnlyList<ToolchainDiagnostic> ValidateTableDeclarations(SelectedModuleContractTable table)
    {
        table = table.ArgNotNull();

        var registry = FromTable(table);
        var nodeKinds = table.AstFacets
            .SelectMany(static facet => facet.AstOwnership)
            .Select(static ownership => ownership.NodeKind)
            .Distinct()
            .OrderBy(static kind => kind.Value, StringComparer.Ordinal)
            .ToArray();

        var diagnostics = new List<ToolchainDiagnostic>();
        foreach (var nodeKind in nodeKinds)
        {
            var resolution = registry.Resolve(nodeKind);
            if (resolution.Outcome == OwnershipOutcome.Competing)
                diagnostics.AddRange(CreateMultipleOwnersDiagnostic(nodeKind, resolution.CompetingOwners));
        }

        return diagnostics
            .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<ToolchainDiagnostic> ValidateNodeOwnership(AstNodeKind nodeKind)
    {
        var resolution = Resolve(nodeKind);
        return resolution.Outcome switch
        {
            OwnershipOutcome.Unowned => CreateZeroOwnerDiagnostic(nodeKind),
            OwnershipOutcome.Competing => CreateMultipleOwnersDiagnostic(nodeKind, resolution.CompetingOwners),
            _ => []
        };
    }

    /// <summary>
    ///     Resolves one node kind into either a lowering set or a conflict.
    /// </summary>
    /// <remarks>
    ///     <see cref="AstOwnershipMode.ObserverOnly" /> and <see cref="AstOwnershipMode.ValidatorOnly" /> declare that
    ///     a module deliberately does <em>not</em> lower the node, so they take part in neither branch. A node declared
    ///     only in those modes therefore has no lowering owner, which is what <c>UT-MOD-OWNERSHIP-001</c> says.
    /// </remarks>
    private OwnershipResolution Resolve(AstNodeKind nodeKind)
    {
        var declarations = _ownership
            .Where(x => x.NodeKind == nodeKind)
            .ToArray();
        var exclusive = declarations
            .Where(static x => x.Mode == AstOwnershipMode.Exclusive)
            .ToArray();
        var cooperative = declarations
            .Where(static x => x.Mode == AstOwnershipMode.Cooperative)
            .ToArray();

        var exclusiveOwners = exclusive
            .Select(static x => x.OwnerModule)
            .Distinct()
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .ToArray();

        // An exclusive owner cannot share, and two exclusive owners cannot both be sole. Either shape is a conflict
        // regardless of which side the lowerer happens to be on.
        if (exclusiveOwners.Length > 1 || (exclusiveOwners.Length == 1 && cooperative.Length > 0))
            return OwnershipResolution.Competing(DescribeCompeting(exclusive, cooperative));

        if (exclusiveOwners.Length == 1)
            return OwnershipResolution.Resolved(exclusiveOwners);

        if (cooperative.Length == 0)
            return OwnershipResolution.Unowned();

        // The cooperating modules are the whole point of the cooperative mode: the declaring owner plus every helper
        // it names may lower the node. Without this the field is write-only and cooperation cannot exist.
        var group = cooperative
            .Select(static x => x.OwnerModule)
            .Concat(cooperative.SelectMany(static x => x.CooperatingModules))
            .Distinct()
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .ToArray();
        return OwnershipResolution.Resolved(group);
    }

    /// <summary>
    ///     Names each competing module together with the mode it declared, because the contradiction is between modes
    ///     and a bare list of module ids does not say what has to change.
    /// </summary>
    private static IReadOnlyList<CompetingOwner> DescribeCompeting(
        IReadOnlyList<AstOwnershipContract> exclusive,
        IReadOnlyList<AstOwnershipContract> cooperative) =>
        exclusive
            .Select(static x => new CompetingOwner(x.OwnerModule, x.Mode))
            .Concat(cooperative.Select(static x => new CompetingOwner(x.OwnerModule, x.Mode)))
            .GroupBy(static x => x.Module)
            .Select(static group => group.OrderBy(static x => x.Mode).First())
            .OrderBy(static x => x.Module.Value, StringComparer.Ordinal)
            .ThenBy(static x => x.Mode)
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
        IReadOnlyList<CompetingOwner> owners)
    {
        var ownerNames = string.Join(", ", owners.Select(static x => $"{x.Module} ({x.Mode})"));
        return
        [
            new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.MultipleAstOwners,
                ToolchainDiagnosticSeverity.Warning,
                $"AST node '{nodeKind}' has multiple declared lowering owners: {ownerNames}.",
                null,
                [new ToolchainDiagnosticHint("Use a single exclusive owner or replace the competing declarations with one cooperative contract that names its helpers in CooperatingModules.")])
        ];
    }

    private static IReadOnlyList<ToolchainDiagnostic> CreateLowererMismatchDiagnostic(
        AstNodeKind nodeKind,
        ModuleId lowererModule,
        IReadOnlyList<ModuleId> loweringModules)
    {
        var ownerNames = string.Join(", ", loweringModules.Select(static x => x.Value));
        return
        [
            new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.LowererOwnershipMismatch,
                ToolchainDiagnosticSeverity.Warning,
                $"Lowerer '{lowererModule}' handles AST node '{nodeKind}', but lowering is declared by '{ownerNames}'.",
                null,
                [new ToolchainDiagnosticHint("Declare matching AST ownership, add the module to CooperatingModules of a cooperative contract, or keep the implementation as observer/validator-only.")])
        ];
    }

    private readonly record struct CompetingOwner(ModuleId Module, AstOwnershipMode Mode);

    private enum OwnershipOutcome
    {
        Resolved,
        Unowned,
        Competing
    }

    private readonly record struct OwnershipResolution(
        OwnershipOutcome Outcome,
        IReadOnlyList<ModuleId> LoweringModules,
        IReadOnlyList<CompetingOwner> CompetingOwners)
    {
        public static OwnershipResolution Resolved(IReadOnlyList<ModuleId> modules) =>
            new(OwnershipOutcome.Resolved, modules, []);

        public static OwnershipResolution Unowned() =>
            new(OwnershipOutcome.Unowned, [], []);

        public static OwnershipResolution Competing(IReadOnlyList<CompetingOwner> owners) =>
            new(OwnershipOutcome.Competing, [], owners);
    }
}
