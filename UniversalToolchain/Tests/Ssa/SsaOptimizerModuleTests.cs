using IntermediateRepresentationAbstractions;
using Tests.Infrastructure;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Ssa.Lowering;
using UniversalToolchain.Ssa.Optimization;
using UniversalToolchain.Wist.LanguagePack;
using UniversalIntermediateRepresentation;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaOptimizerModuleTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void ProcessIr_WhenAirUsesSupportedSubset_ReturnsVerifiableAir()
    {
        var source = new AbstractIR();
        source.Push(42.5);

        var result = new SsaOptimizerModule().Optimize(source);

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
            new SsaOptimizerModule().Optimize(source));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerException, Is.TypeOf<AirToSsaConversionException>());
            Assert.That(exception.Diagnostics, Is.Not.Empty);
            Assert.That(exception.Report.UsedSsa, Is.False);
            Assert.That(exception.Report.FellBackToInput, Is.False);
        });
    }

    [Test]
    public void Dsl_WithSsaOptimizerDirective_SelectsCanonicalSsaContribution()
    {
        var (package, plan) = Compile(
            """
            dialect Ssa
            use Arithmetic,Numbers,Scopes,Whitespaces
            backend interpreter
            enable Ssa
            """);

        var selectedTypes = WistRuntimeComponentCatalog.GetSelectedImplementationTypes(plan);

        Assert.Multiple(() =>
        {
            Assert.That(
                plan.Contributions.Select(static contribution => contribution.Contribution.Id),
                Does.Contain(WistContributionIds.SsaOptimizer));
            Assert.That(selectedTypes, Does.Contain(typeof(SsaOptimizerModule)));
        });
        GC.KeepAlive(package);
    }

    [Test]
    public void Run_WithSsaOptimizerDirectiveAndSupportedBooleanProgram_ExecutesThroughInterpreter()
    {
        var (package, plan) = Compile(
            """
            dialect Ssa
            use BooleanConditions,Conditions,Scopes,Whitespaces
            backend interpreter
            enable Ssa
            """);
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });

        var result = runtime.Run(new LanguageExecutionRequest("true", Interpreter)).Value;

        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Run_WithSsaOptimizerDirectiveAndManagedNumericLiteral_ReturnsNumericValue()
    {
        var (package, plan) = Compile(
            """
            dialect Ssa
            use Arithmetic,Numbers,Scopes,Whitespaces
            backend interpreter
            enable Ssa
            """);
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });

        var result = runtime.Run(new LanguageExecutionRequest("42", Interpreter)).Value;
        var normalized = WistRuntimeValueAdapterActivation.Normalize(plan, result);

        Assert.That(BackendValueNormalizer.Normalize(normalized), Is.EqualTo(42.0));
    }

    private static (WistLanguageFeaturePackage Package, LanguagePlan Plan) Compile(string source)
    {
        var package = new WistLanguageFeaturePackage();
        var definition = WistFacadeLanguageDefinitionFactory.FromDialectText(
            source,
            "ssa.wistdialect",
            Interpreter.Value,
            WistFacadeSsaPolicy.Require);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        return (package, plan);
    }
}
