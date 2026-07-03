using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ModuleContractTableBuilderTests
{
    [Test]
    public void Build_WhenFacetsAreAddedOutOfOrder_NormalizesDeterministically()
    {
        var firstModule = new ModuleId("core.first");
        var secondModule = new ModuleId("core.second");

        var table = new ModuleContractTableBuilder()
            .AddFacet(CreateAstFacet(secondModule, "core.ast.second"))
            .AddFacet(CreateSyntaxFacet(firstModule, "Number"))
            .AddFacet(CreateAstFacet(firstModule, "core.ast.first"))
            .Build();

        var ordered = table.Facets
            .Select(static x => $"{x.ModuleId.Value}:{x.Kind}")
            .ToArray();

        Assert.That(
            ordered,
            Is.EqualTo(new[]
            {
                "core.first:Syntax",
                "core.first:Ast",
                "core.second:Ast"
            }));
        Assert.That(table.Diagnostics, Is.Empty);
    }

    [Test]
    public void Build_WhenModuleDeclaresDuplicateFacetKind_ReportsDiagnostic()
    {
        var moduleId = new ModuleId("core.duplicate");

        var table = new ModuleContractTableBuilder()
            .AddFacet(CreateAstFacet(moduleId, "core.ast.one"))
            .AddFacet(CreateAstFacet(moduleId, "core.ast.two"))
            .Build();

        Assert.That(table.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(table.Diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.DuplicateFacet));
        Assert.That(table.Diagnostics[0].Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Error));
    }

    [Test]
    public void Build_WhenEveryFacetKindHasExplicitOrder_DoesNotReportOrderDiagnostic()
    {
        var table = new ModuleContractTableBuilder().Build();

        Assert.That(
            table.Diagnostics.Select(static x => x.Code),
            Does.Not.Contain(ModuleContractDiagnosticCodes.MissingFacetKindOrder));
    }

    private static AstContractFacet CreateAstFacet(ModuleId moduleId, string nodeKind) =>
        new(
            moduleId,
            [
                new AstOwnershipContract(
                    new AstNodeKind(nodeKind),
                    AstOwnershipMode.Exclusive,
                    moduleId,
                    [])
            ]);

    private static SyntaxContractFacet CreateSyntaxFacet(ModuleId moduleId, string lexemeId) =>
        new(
            moduleId,
            [
                new LexemeContract(lexemeId, "test pattern")
            ],
            []);
}
