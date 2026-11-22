using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

/// <summary>
///     Pins who may lower an AST node kind, including the cooperative case that was impossible before.
/// </summary>
/// <remarks>
///     <see cref="AstOwnershipContract.CooperatingModules" /> was declared, null-checked, copied into every runtime
///     snapshot and read by nothing, so a contract saying <c>Cooperative</c> and naming a helper still reported the
///     helper as an intruder. The matching half of the same bug made <c>UT-MOD-OWNERSHIP-002</c> fire on two modules
///     that declared cooperative lowering of one node - the exact shape cooperation exists for.
///     <para>
///         These tests distinguish cooperative from exclusive rather than just exercising the success path: every
///         positive case is paired with the case that must still fail, so deleting the cooperation branch or deleting
///         the conflict branch each kills a different test.
///     </para>
/// </remarks>
[TestFixture]
public sealed class AstOwnershipCooperativeTests
{
    private static readonly ModuleId Owner = new("coop.owner");
    private static readonly ModuleId Helper = new("coop.helper");
    private static readonly AstNodeKind Node = new("coop.ast.node");

    [Test]
    public void ValidateLowerer_WhenHelperIsNamedInCooperatingModules_ReturnsNoDiagnostics()
    {
        var registry = RegistryFor(Cooperative(Owner, Node, [Helper]));

        var diagnostics = registry.ValidateLowerer(new StubLowerer(Helper, Node));

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ValidateLowerer_WhenCooperativeHelperIsNotNamed_ReturnsOwnershipMismatch()
    {
        // The control for the test above: dropping the cooperation branch must not turn every lowerer into a valid one.
        var stranger = new ModuleId("coop.stranger");
        var registry = RegistryFor(Cooperative(Owner, Node, [Helper]));

        var diagnostics = registry.ValidateLowerer(new StubLowerer(stranger, Node));

        Assert.That(diagnostics.Select(static x => x.Code),
            Has.Exactly(1).EqualTo(ModuleContractDiagnosticCodes.LowererOwnershipMismatch));
        Assert.That(diagnostics[0].Message, Does.Contain(stranger.Value));
        Assert.That(diagnostics[0].Message, Does.Contain(Owner.Value));
    }

    [Test]
    public void ValidateLowerer_WhenCooperativeOwnerLowersWithoutHelpers_ReturnsNoDiagnostics()
    {
        var registry = RegistryFor(Cooperative(Owner, Node, []));

        Assert.That(registry.ValidateLowerer(new StubLowerer(Owner, Node)), Is.Empty);
    }

    [Test]
    public void ValidateNodeOwnership_WhenTwoModulesDeclareMutualCooperativeOwnership_ReturnsNoDiagnostics()
    {
        // Two cooperative declarations of one node used to be reported as a conflict, which made the documented
        // remedy for an ownership conflict unusable.
        var registry = RegistryFor(
            Cooperative(Owner, Node, [Helper]),
            Cooperative(Helper, Node, [Owner]));

        Assert.That(registry.ValidateNodeOwnership(Node), Is.Empty);
    }

    [Test]
    public void ValidateLowerer_WhenMutualCooperativeOwnershipIsDeclared_AcceptsBothModules()
    {
        var registry = RegistryFor(
            Cooperative(Owner, Node, [Helper]),
            Cooperative(Helper, Node, [Owner]));

        Assert.Multiple(() =>
        {
            Assert.That(registry.ValidateLowerer(new StubLowerer(Owner, Node)), Is.Empty);
            Assert.That(registry.ValidateLowerer(new StubLowerer(Helper, Node)), Is.Empty);
        });
    }

    [Test]
    public void ValidateNodeOwnership_WhenCooperativeOwnershipIsDeclaredOnlyByOneModule_StillAcceptsItsHelper()
    {
        // Cooperation is one-directional in the declaration and two-directional in effect: the owner names the helper,
        // the helper does not have to declare anything back.
        var registry = RegistryFor(Cooperative(Owner, Node, [Helper]));

        Assert.Multiple(() =>
        {
            Assert.That(registry.ValidateNodeOwnership(Node), Is.Empty);
            Assert.That(registry.ValidateLowerer(new StubLowerer(Helper, Node)), Is.Empty);
        });
    }

    [Test]
    public void ValidateNodeOwnership_WhenExclusiveAndCooperativeDeclarationsShareANode_ReportsMultipleOwners()
    {
        // An exclusive owner cannot also share. This is the shape that used to look resolvable because the resolver
        // only counted exclusive declarations and silently ignored the cooperative one.
        var registry = RegistryFor(
            Exclusive(Owner, Node),
            Cooperative(Helper, Node, [Owner]));

        var diagnostics = registry.ValidateNodeOwnership(Node);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.MultipleAstOwners));
        Assert.That(diagnostics[0].Message, Does.Contain("Exclusive"));
        Assert.That(diagnostics[0].Message, Does.Contain("Cooperative"));
    }

    [Test]
    public void ValidateLowerer_WhenExclusiveAndCooperativeDeclarationsShareANode_ReportsMultipleOwners()
    {
        var registry = RegistryFor(
            Exclusive(Owner, Node),
            Cooperative(Helper, Node, [Owner]));

        var diagnostics = registry.ValidateLowerer(new StubLowerer(Owner, Node));

        Assert.That(diagnostics.Select(static x => x.Code),
            Has.Exactly(1).EqualTo(ModuleContractDiagnosticCodes.MultipleAstOwners));
    }

    [Test]
    public void ValidateLowerer_WhenNodeIsDeclaredObserverOnly_ReportsZeroOwnerRatherThanAcceptingTheLowerer()
    {
        // ObserverOnly and ValidatorOnly say "this module deliberately does not lower the node". A lowerer for that node
        // therefore has no owner - not a matching contract. Before the resolution was unified, these modes were simply
        // filtered out, which produced the same code by accident and without the second statement being written down.
        var registry = RegistryFor(new AstOwnershipContract(Node, AstOwnershipMode.ObserverOnly, Owner, []));

        var diagnostics = registry.ValidateLowerer(new StubLowerer(Owner, Node));

        Assert.That(diagnostics.Select(static x => x.Code),
            Has.Exactly(1).EqualTo(ModuleContractDiagnosticCodes.ZeroAstOwner));
    }

    [Test]
    public void ValidateLowerer_WhenNodeIsDeclaredNowhere_ReportsZeroOwner()
    {
        var registry = RegistryFor(Cooperative(Owner, Node, [Helper]));

        var diagnostics = registry.ValidateLowerer(new StubLowerer(Owner, new AstNodeKind("coop.ast.absent")));

        Assert.That(diagnostics.Select(static x => x.Code),
            Has.Exactly(1).EqualTo(ModuleContractDiagnosticCodes.ZeroAstOwner));
    }

    [Test]
    public void Build_WhenTwoModulesClaimOneNodeExclusively_ReportsMultipleAstOwners()
    {
        // The production path, not the registry in isolation. A test against the registry alone stays green if the
        // call disappears from the builder - which is exactly how UT-CONTRACT-002 stayed invisible for so long.
        var other = new ModuleId("coop.other");
        var table = TableFor(Exclusive(Owner, Node), Exclusive(other, Node));

        var diagnostics = table.Diagnostics
            .Where(static x => x.Code == ModuleContractDiagnosticCodes.MultipleAstOwners)
            .ToArray();

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Warning));
        Assert.That(diagnostics[0].Message, Does.Contain(Owner.Value));
        Assert.That(diagnostics[0].Message, Does.Contain(other.Value));
    }

    [Test]
    public void Build_WhenTwoModulesDeclareMutualCooperativeOwnership_ReportsNoAstOwnerDiagnostic()
    {
        // The control that stops the wiring from being satisfied by "report everything".
        var table = TableFor(Cooperative(Owner, Node, [Helper]), Cooperative(Helper, Node, [Owner]));

        Assert.That(
            table.Diagnostics.Select(static x => x.Code),
            Has.None.EqualTo(ModuleContractDiagnosticCodes.MultipleAstOwners));
    }

    [Test]
    public void Build_WhenNoNodeIsContested_ReportsNoAstOwnerDiagnostic()
    {
        var table = TableFor(Exclusive(Owner, Node), Cooperative(Helper, new AstNodeKind("coop.ast.other"), [Owner]));

        Assert.That(
            table.Diagnostics.Select(static x => x.Code),
            Has.None.EqualTo(ModuleContractDiagnosticCodes.MultipleAstOwners));
    }

    private static UniversalToolchain.ModuleContracts.SelectedModuleContractTable TableFor(
        params AstOwnershipContract[] contracts) =>
        new ModuleContractTableBuilder()
            .AddFacets(contracts
                .GroupBy(static x => x.OwnerModule)
                .Select(group => (IModuleContractFacet)new AstContractFacet(group.Key, group.ToArray())))
            .Build();

    private static AstOwnershipRegistry RegistryFor(params AstOwnershipContract[] contracts) =>
        AstOwnershipRegistry.FromTable(
            new ModuleContractTableBuilder()
                .AddFacets(contracts
                    .GroupBy(static x => x.OwnerModule)
                    .Select(group => (IModuleContractFacet)new AstContractFacet(group.Key, group.ToArray())))
                .Build());

    private static AstOwnershipContract Exclusive(ModuleId module, AstNodeKind node) =>
        new(node, AstOwnershipMode.Exclusive, module, []);

    private static AstOwnershipContract Cooperative(
        ModuleId module,
        AstNodeKind node,
        IReadOnlyList<ModuleId> cooperating) =>
        new(node, AstOwnershipMode.Cooperative, module, cooperating);

    private sealed class StubLowerer(ModuleId moduleId, AstNodeKind nodeKind) : IAstNodeLowerer
    {
        public ModuleId ModuleId { get; } = moduleId;

        public AstNodeKind NodeKind { get; } = nodeKind;

        public LoweringResult Lower(AstNode node, AstNodeLoweringContext context) => new(context.Bytecode, []);
    }
}
