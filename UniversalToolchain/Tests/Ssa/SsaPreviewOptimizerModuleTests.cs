using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Lowering;
using UniversalToolchain.Ssa.Optimization;
using UniversalIntermediateRepresentation;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaPreviewOptimizerModuleTests
{
    [Test]
    public void ProcessIr_WhenAirUsesSupportedSubset_ReturnsVerifiableAir()
    {
        var source = new AbstractIR();
        source.Push(42.5);

        var result = new SsaPreviewOptimizerModule().ProcessIr(source, new PassthroughCompiler());

        Assert.Multiple(() =>
        {
            Assert.That(result.Instructions.Select(static x => x.UOpCode), Does.Contain(UOpCode.Push));
            Assert.That(
                result.Instructions.Where(static x => x.UOpCode == UOpCode.Push).SelectMany(static x => x.Operands),
                Does.Contain(42.5));
        });
    }

    [Test]
    public void ProcessIr_WhenAirUsesUnsupportedIntrinsic_ThrowsWithoutFallingBackToInputAir()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var exception = Assert.Throws<SsaRouteException>(() =>
            new SsaPreviewOptimizerModule().ProcessIr(source, new PassthroughCompiler()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerException, Is.TypeOf<AirToSsaConversionException>());
            Assert.That(exception.Diagnostics, Is.Not.Empty);
            Assert.That(exception.Report.UsedSsa, Is.False);
            Assert.That(exception.Report.FellBackToInput, Is.False);
        });
    }

    [Test]
    public void ComposeText_WithSsaOptimizerDirective_ResolvesManifestOptimizer()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeText(
            """
            dialect SsaPreview
            use Arithmetic,Numbers,Scopes,Whitespaces
            backend interpreter
            enable Ssa
            """,
            "ssa-preview");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        using var host = workflow.CreateHost(composition);

        Assert.Multiple(() =>
        {
            Assert.That(selection.EnabledOptimizers.Select(static x => x.CanonicalAlias), Does.Contain("Ssa"));
            Assert.That(host.Configuration.Optimizers, Does.Contain(typeof(SsaPreviewOptimizerModule)));
        });
    }

    [Test]
    public void Run_WithSsaOptimizerDirectiveAndSupportedBooleanProgram_ExecutesThroughInterpreter()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            """
            dialect SsaPreview
            use BooleanConditions,Conditions,Scopes,Whitespaces
            backend interpreter
            enable Ssa
            """,
            "ssa-preview");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var host = workflow.CreateHost(composition);
        var result = host.Run("true", "interpreter");

        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Run_WithSsaOptimizerDirectiveAndManagedNumericLiteral_ReturnsNumericValue()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            """
            dialect SsaPreview
            use Arithmetic,Numbers,Scopes,Whitespaces
            backend interpreter
            enable Ssa
            """,
            "ssa-preview");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var host = workflow.CreateHost(composition);
        var result = host.Run("42", "interpreter");

        Assert.That(BackendValueNormalizer.Normalize(result), Is.EqualTo(42.0));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    private static string FormatComposition(DialectFrameworkCompositionResult composition) =>
        DialectCompositionExplanationFormatter.FormatDeterministic(
            DialectCompositionExplanationProjector.Project(composition));

    private sealed class PassthroughCompiler : IAbstractIrCompiler<IAbstractIR>
    {
        public IAbstractIR Compile(IAbstractIR air, CompilationInput input) => air;
    }
}
