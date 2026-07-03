using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class AstOwnershipLoweringAdapterTests
{
    [Test]
    public void Lower_WhenLegacyVisitorIsWrapped_RunsExistingVisitorAgainstSameBytecode()
    {
        var moduleId = new ModuleId("wist.test");
        var nodeKind = new AstNodeKind("wist.test.ast.node");
        var bytecode = new Bytecode([]);
        var adapter = new LegacyAstVisitorLoweringAdapter(moduleId, nodeKind, new RecordingVisitor());
        var context = new AstNodeLoweringContext(new NoopTranslator(bytecode), bytecode);

        var result = adapter.Lower(CreateNode("LegacyNode"), context);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Bytecode.Instructions, Has.Count.EqualTo(1));
        Assert.That(result.Bytecode.Instructions[0].Ops.SelectMany(static x => x.Value).Single().Name, Is.EqualTo("legacy-record"));
    }

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

    private sealed class RecordingVisitor : IAstVisitor
    {
        public void TryVisit(BytecodeVisitorData data)
        {
            data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl("legacy-record", (_, _) => { })));
        }
    }

    private sealed class NoopTranslator(Bytecode bytecode) : IAstToBytecodeTranslator
    {
        public BytecodeTranslatorConfiguration Configuration { get; } = new([]);

        public Bytecode Translate(AstNode root) => bytecode;
    }

    private sealed class StubLowerer(ModuleId moduleId, AstNodeKind nodeKind) : IAstNodeLowerer
    {
        public ModuleId ModuleId { get; } = moduleId;

        public AstNodeKind NodeKind { get; } = nodeKind;

        public LoweringResult Lower(AstNode node, AstNodeLoweringContext context) => new(context.Bytecode, []);
    }
}
