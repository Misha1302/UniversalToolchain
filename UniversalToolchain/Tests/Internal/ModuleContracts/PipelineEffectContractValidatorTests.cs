using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

/// <summary>
///     Covers the diagnostics <see cref="PipelineEffectContractValidator" /> emits during
///     <see cref="ModuleContractTableBuilder.Build" />.
/// </summary>
/// <remarks>
///     Both rules live in the production table build, so these tests drive the real <c>Build()</c> rather than the
///     validator in isolation: a test against the validator alone would stay green if the call disappeared from the
///     builder, which is exactly the failure mode that hid <c>UT-CONTRACT-002</c> for so long.
/// </remarks>
[TestFixture]
public sealed class PipelineEffectContractValidatorTests
{
    private static readonly ModuleId Module = new("test.pipeline.effects");

    [Test]
    public void Build_WhenEffectReferencesUndeclaredFact_ReportsUnknownCompilerFact()
    {
        var undeclared = new CompilerFactId("test.fact.undeclared");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new PipelineEffectFacet(
                Module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.effect.undeclared"),
                        CompilerPipelineStage.Bytecode,
                        [undeclared],
                        [],
                        [],
                        [])
                ]))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.UnknownCompilerFact));
    }

    [Test]
    public void Build_WhenEffectProducesAndInvalidatesTheSameFact_ReportsInvalidPipelineEffect()
    {
        var fact = new CompilerFactId("test.fact.contradictory");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(Module, [new CompilerFactOwnershipContract(fact, Module)]))
            .AddFacet(new PipelineEffectFacet(
                Module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.effect.contradictory"),
                        CompilerPipelineStage.Bytecode,
                        [],
                        [fact],
                        [],
                        [fact])
                ]))
            .Build();

        var diagnostics = table.Diagnostics
            .Where(x => x.Code == ModuleContractDiagnosticCodes.InvalidPipelineEffect)
            .ToArray();

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Message, Does.Contain("both produces and invalidates"));
    }

    [Test]
    public void Build_WhenEffectPreservesAndInvalidatesTheSameFact_ReportsInvalidPipelineEffect()
    {
        var fact = new CompilerFactId("test.fact.preserve-invalidate");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(Module, [new CompilerFactOwnershipContract(fact, Module)]))
            .AddFacet(new PipelineEffectFacet(
                Module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.effect.preserve-invalidate"),
                        CompilerPipelineStage.Bytecode,
                        [],
                        [],
                        [fact],
                        [fact])
                ]))
            .Build();

        var diagnostics = table.Diagnostics
            .Where(x => x.Code == ModuleContractDiagnosticCodes.InvalidPipelineEffect)
            .ToArray();

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Message, Does.Contain("both preserves and invalidates"));
    }

    [Test]
    public void Build_WhenEffectDeclaresConsistentFacts_ReportsNeitherCode()
    {
        var fact = new CompilerFactId("test.fact.consistent");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(Module, [new CompilerFactOwnershipContract(fact, Module)]))
            .AddFacet(new PipelineEffectFacet(
                Module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.effect.consistent"),
                        CompilerPipelineStage.Bytecode,
                        [],
                        [fact],
                        [],
                        [])
                ]))
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(
                table.Diagnostics.Select(static x => x.Code),
                Does.Not.Contain(ModuleContractDiagnosticCodes.InvalidPipelineEffect));
            Assert.That(
                table.Diagnostics.Select(static x => x.Code),
                Does.Not.Contain(ModuleContractDiagnosticCodes.UnknownCompilerFact));
        });
    }
}
