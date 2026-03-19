using CommonExceptions;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDslRegressionTests
{
    [Test]
    public void SingletonDirectiveEnforcement_ShouldBeCentralized_ForCustomFeaturesInDiAndStandalonePipelines()
    {
        var customCompiler = DialectDslTestComposition.CreateCompiler(services => services.AddDialectDirectiveFeature<SingletonNoteDirectiveFeature>());
        var baseRegistry = DialectDslTestComposition.CreateRegistry();
        var standaloneLikeCompiler = new DialectDslCompiler(new DialectDslFrontendModule(new DialectDslRegistry(
            baseRegistry.DirectiveFeatures.Concat([new SingletonNoteDirectiveFeature()]),
            baseRegistry.DocumentRules)));

        var diException = Assert.Throws<ParserException>(() => customCompiler.Compile("dialect Demo\nnote first\nnote second\n"));
        var standaloneException = Assert.Throws<ParserException>(() => standaloneLikeCompiler.Compile("dialect Demo\nnote first\nnote second\n"));

        Assert.Multiple(() =>
        {
            DialectDslTestSupport.AssertParserExceptionContains(diException!, "note directive can only be declared once");
            DialectDslTestSupport.AssertParserExceptionContains(standaloneException!, "note directive can only be declared once");
        });
    }

    [Test]
    public void IntrinsicAndOptimizerPolicies_ShouldRemainSeparated_WhenDirectiveNamesOverlap()
    {
        var slice = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nallow shared\nenable shared\nforbid shared-intrinsic\ndisable shared-optimizer\n");

        Assert.Multiple(() =>
        {
            Assert.That(slice.IntrinsicDirectives.Select(x => (x.Name, x.Allowed)), Is.EqualTo(new[]
            {
                ("shared", true),
                ("shared-intrinsic", false)
            }));
            Assert.That(slice.OptimizerDirectives.Select(x => (x.Name, x.Enabled)), Is.EqualTo(new[]
            {
                ("shared", true),
                ("shared-optimizer", false)
            }));
        });
    }

    [Test]
    public void AirReader_ShouldIgnoreUnrelatedMetadata_WhilePreservingDialectAnnotationsAcrossInstructions()
    {
        var air = new UniversalIntermediateRepresentation.AbstractIR();
        air.AppendInstructions([
            new Instruction(UOpCode.Annotate, metadata: [new object(), new DialectNameAirAnnotation("Demo")]),
            new Instruction(UOpCode.Annotate, metadata: [new SecurityAirAnnotation(DialectSecurityProfile.Restricted), new object()]),
            new Instruction(UOpCode.Annotate, metadata: [new CapabilityAirAnnotation(["sandbox"]), new UseModulesAirAnnotation(["Arithmetic"])])
        ]);

        var slice = DialectDefinitionSliceAirReader.Read(air);

        Assert.Multiple(() =>
        {
            Assert.That(slice.Name, Is.EqualTo("Demo"));
            Assert.That(slice.SecurityProfile, Is.EqualTo(DialectSecurityProfile.Restricted));
            Assert.That(slice.CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[] { "sandbox" }));
            Assert.That(slice.UseModules, Is.EqualTo(new[] { "Arithmetic" }));
        });
    }

    [Test]
    public void AirReader_ShouldRejectNullMetadataEntries_WithMeaningfulError()
    {
        var air = new UniversalIntermediateRepresentation.AbstractIR();
        air.AppendInstructions([
            new Instruction(UOpCode.Annotate, metadata: [new DialectNameAirAnnotation("Demo"), null!])
        ]);

        var ex = Assert.Throws<InvalidOperationException>(() => DialectDefinitionSliceAirReader.Read(air));

        Assert.That(ex!.Message, Does.Contain("null annotation entry"));
    }

    [Test]
    public void AirReader_ShouldRejectDuplicateSingletonSecurityAnnotations()
    {
        var air = new UniversalIntermediateRepresentation.AbstractIR();
        air.AppendInstructions([
            new Instruction(UOpCode.Annotate, metadata: [new DialectNameAirAnnotation("Demo"), new SecurityAirAnnotation(DialectSecurityProfile.Trusted)]),
            new Instruction(UOpCode.Annotate, metadata: [new SecurityAirAnnotation(DialectSecurityProfile.Restricted)])
        ]);

        var ex = Assert.Throws<InvalidOperationException>(() => DialectDefinitionSliceAirReader.Read(air));

        Assert.That(ex!.Message, Does.Contain("duplicate singleton annotation").And.Contain(nameof(SecurityAirAnnotation)));
    }

    [Test]
    public void RestrictedSecurityProfile_WithUnsafeInteropCapability_ShouldProduceSecurityDiagnostic()
    {
        var slice = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nsecurity restricted\ncapability unsafe-interop\n");

        var plan = new DialectCompiledDialectBuildPlanBuilder().Build(slice);

        Assert.That(plan.ValidationResult.Diagnostics.Select(x => (x.Code, x.Message, x.Severity)).ToArray(), Is.EqualTo(new[]
        {
            ("S006", "Capability 'unsafe-interop' cannot be enabled under restricted security profile.", DialectDiagnosticSeverity.Error)
        }));
    }

    [Test]
    public void TrustedSecurityProfile_WithUnsafeInteropCapability_ShouldNotProduceSecurityDiagnostic()
    {
        var slice = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nsecurity trusted\ncapability unsafe-interop\n");

        var plan = new DialectCompiledDialectBuildPlanBuilder().Build(slice);

        Assert.That(plan.ValidationResult.Diagnostics, Is.Empty);
    }

    [Test]
    public void RegistryFactory_ShouldRejectNullProviderEntries_RatherThanSilentlyIgnoringThem()
    {
        var nullProviders = new List<IDialectDslFeatureProvider> { null! };
        var factory = new DialectDslRegistryFactory([], [], nullProviders);

        var ex = Assert.Throws<ArgumentException>(() => factory.CreateRegistry());

        Assert.That(ex!.Message, Does.Contain("Collection must not contain null values"));
    }

    [Test]
    public void ValidationContext_ShouldRejectNullOrWhitespaceStateKeys()
    {
        var accumulation = new DialectDirectiveAccumulation();
        var validationContext = new DialectDirectiveValidationContext();

        var accumulationEx = Assert.Throws<ArgumentException>(() => accumulation.GetOrCreateList(new DialectListStateKey<string>(" ")));
        var validationEx = Assert.Throws<ArgumentException>(() => validationContext.GetOrAddState(new DialectValueStateKey<List<string>>(""), static () => []));

        Assert.Multiple(() =>
        {
            Assert.That(accumulationEx!.Message, Does.Contain("must not be empty"));
            Assert.That(validationEx!.Message, Does.Contain("must not be empty"));
        });
    }
}
