using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class AstOwnershipLoweringAdapterTests
{
    [Test]
    public void ValidateLowerer_WhenOwnershipMatches_ReturnsNoDiagnostics()
    {
        var moduleId = new ModuleId("wist.test");
        var nodeKind = new AstNodeKind("wist.test.ast.node");
        var table = new ModuleContractTableBuilder()
            .AddFacet(CreateAstFacet(moduleId, nodeKind))
            .Build();
        var registry = AstOwnershipRegistry.FromTable(table);

        var diagnostics = registry.ValidateLowerer(new StubLowerer(moduleId, nodeKind));

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ValidateNodeOwnership_WhenNodeHasNoOwner_ReturnsWarningDiagnostic()
    {
        var table = new ModuleContractTableBuilder().Build();
        var registry = AstOwnershipRegistry.FromTable(table);

        var diagnostics = registry.ValidateNodeOwnership(new AstNodeKind("wist.missing.ast.node"));

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.ZeroAstOwner));
    }

    [Test]
    public void ValidateNodeOwnership_WhenNodeHasMultipleOwners_ReturnsWarningDiagnostic()
    {
        var nodeKind = new AstNodeKind("wist.shared.ast.node");
        var table = new ModuleContractTableBuilder()
            .AddFacet(CreateAstFacet(new ModuleId("wist.first"), nodeKind))
            .AddFacet(CreateAstFacet(new ModuleId("wist.second"), nodeKind))
            .Build();
        var registry = AstOwnershipRegistry.FromTable(table);

        var diagnostics = registry.ValidateNodeOwnership(nodeKind);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.MultipleAstOwners));
    }

    [Test]
    public void ValidateLowerer_WhenAnotherModuleOwnsTheNode_ReturnsOwnershipMismatchDiagnostic()
    {
        // The node is owned exclusively by a different module, so this lowerer is handling another module's node.
        // That is the only way UT-MOD-OWNERSHIP-003 can fire, and nothing else in the suite reached it.
        var owner = new ModuleId("wist.owner");
        var intruder = new ModuleId("wist.intruder");
        var nodeKind = new AstNodeKind("wist.owner.ast.node");
        var table = new ModuleContractTableBuilder()
            .AddFacet(CreateAstFacet(owner, nodeKind))
            .Build();
        var registry = AstOwnershipRegistry.FromTable(table);

        var diagnostics = registry.ValidateLowerer(new StubLowerer(intruder, nodeKind));

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.LowererOwnershipMismatch));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Warning));
    }

    [Test]
    public void ValidateLowerer_WhenNodeIsCooperativelyOwnedByAnotherModule_ReturnsOwnershipMismatchDiagnostic()
    {
        // Cooperative ownership is still ownership: a lowerer that is not the declaring module may not claim the
        // node. This pins the second half of the mode check, which the exclusive case above cannot distinguish.
        var owner = new ModuleId("wist.cooperative.owner");
        var intruder = new ModuleId("wist.cooperative.intruder");
        var nodeKind = new AstNodeKind("wist.cooperative.ast.node");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new AstContractFacet(
                owner,
                [new AstOwnershipContract(nodeKind, AstOwnershipMode.Cooperative, owner, [])]))
            .Build();
        var registry = AstOwnershipRegistry.FromTable(table);

        var diagnostics = registry.ValidateLowerer(new StubLowerer(intruder, nodeKind));

        Assert.That(diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.LowererOwnershipMismatch));
    }

    private static AstNode CreateNode(string nodeType) =>
        new(ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType), null, []);

    private static AstContractFacet CreateAstFacet(ModuleId moduleId, AstNodeKind nodeKind) =>
        new(
            moduleId,
            [
                new AstOwnershipContract(
                    nodeKind,
                    AstOwnershipMode.Exclusive,
                    moduleId,
                    [])
            ]);

    private sealed class StubLowerer(ModuleId moduleId, AstNodeKind nodeKind) : IAstNodeLowerer
    {
        public ModuleId ModuleId { get; } = moduleId;

        public AstNodeKind NodeKind { get; } = nodeKind;

        public LoweringResult Lower(AstNode node, AstNodeLoweringContext context) => new(context.Bytecode, []);
    }
}
