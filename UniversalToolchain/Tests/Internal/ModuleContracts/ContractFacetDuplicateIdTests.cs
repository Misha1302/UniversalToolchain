using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ContractFacetDuplicateIdTests
{
    private static readonly ModuleId Module = new("core.duplicate.ids");

    [Test]
    public void Build_WhenFacetDeclaresSameParserNodeTwice_ReportsDuplicateId()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new SyntaxContractFacet(
                Module,
                [],
                [
                    new ParserNodeContract(new AstNodeKind("core.ast.repeated"), 1d, []),
                    new ParserNodeContract(new AstNodeKind("core.ast.repeated"), 2d, [])
                ]))
            .Build();

        var duplicates = table.Diagnostics
            .Where(d => d.Code == ModuleContractDiagnosticCodes.DuplicateId)
            .ToArray();

        Assert.That(duplicates, Has.Length.EqualTo(1));
        Assert.That(duplicates[0].Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Error));
        Assert.That(duplicates[0].Message, Does.Contain("core.ast.repeated"));
    }

    [Test]
    public void Build_WhenFacetDeclaresSameCompilerFactTwice_ReportsDuplicateId()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                Module,
                [
                    new CompilerFactOwnershipContract(new CompilerFactId("core.fact.repeated"), Module),
                    new CompilerFactOwnershipContract(new CompilerFactId("core.fact.repeated"), Module)
                ]))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static d => d.Code),
            Has.Exactly(1).EqualTo(ModuleContractDiagnosticCodes.DuplicateId));
    }

    [Test]
    public void Build_WhenDifferentFacetsOfOneModuleMentionTheSameId_ReportsNothing()
    {
        // Measured on the real corpus: LabelsModule declares wist.labels.ast.label in its Syntax facet (a node it
        // produces) and in its Ast facet (a node it owns). Those are different statements about one id, not a
        // contradiction, so a cross-facet rule would reject the current correct corpus.
        var node = new AstNodeKind("core.ast.shared");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new SyntaxContractFacet(Module, [], [new ParserNodeContract(node, 1d, [])]))
            .AddFacet(new AstContractFacet(
                Module,
                [new AstOwnershipContract(node, AstOwnershipMode.Exclusive, Module, [])]))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static d => d.Code),
            Does.Not.Contain(ModuleContractDiagnosticCodes.DuplicateId));
    }

    [Test]
    public void Build_WhenTwoModulesOwnTheSameNodeExclusively_ReportsNothing()
    {
        // Ownership collision already has an owner: AstOwnershipRegistry reports UT-MOD-OWNERSHIP-002 and the
        // resolution depends on the lowerer, which the contract table cannot see. Duplicating that as a table-time
        // duplicate-id error would invent a second, conflicting answer.
        var first = new ModuleId("core.owner.one");
        var second = new ModuleId("core.owner.two");
        var node = new AstNodeKind("core.ast.contested");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new AstContractFacet(first, [new AstOwnershipContract(node, AstOwnershipMode.Exclusive, first, [])]))
            .AddFacet(new AstContractFacet(second, [new AstOwnershipContract(node, AstOwnershipMode.Exclusive, second, [])]))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static d => d.Code),
            Does.Not.Contain(ModuleContractDiagnosticCodes.DuplicateId));
    }

    [Test]
    public void Build_WhenTwoEmissionsMayEmitTheSameTag_ReportsNoDuplicateId()
    {
        // MayEmitTags is a per-emission capability, not a declaration: "this node may emit the tag". Two different
        // nodes emitting the same tag is an ordinary contract. Counting capabilities as declarations would reject
        // correct contracts - and it did, which is why this case exists.
        var tag = new BytecodeTagId("core.bytecode.shared-tag");
        var astA = new AstNodeKind("core.ast.a");
        var astB = new AstNodeKind("core.ast.b");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new BytecodeContractFacet(
                Module,
                [
                    new BytecodeEmissionContract(astA, [tag], [], new StackEffect(0, 0), SideEffectPolicy.Pure),
                    new BytecodeEmissionContract(astB, [tag], [], new StackEffect(0, 0), SideEffectPolicy.Pure)
                ]))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static d => d.Code),
            Does.Not.Contain(ModuleContractDiagnosticCodes.DuplicateId));
    }

    [Test]
    public void Build_WhenNoFacetRepeatsAnIdentifier_ReportsNoDuplicateId()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new SyntaxContractFacet(Module, [], [new ParserNodeContract(new AstNodeKind("core.ast.unique"), 1d, [])]))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static d => d.Code),
            Does.Not.Contain(ModuleContractDiagnosticCodes.DuplicateId));
    }
}
