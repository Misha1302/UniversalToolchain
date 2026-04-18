using BasicCore.Contracts;
using CommonExceptions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Frontend.Composition;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDslCompositionParityTests
{
    [Test]
    public void AddDialectDsl_ShouldRegisterCoreServices_WithoutBuiltIns()
    {
        var services = new ServiceCollection();
        services.AddDialectDsl();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<DialectDslRegistry>();
        var frontendModule = provider.GetRequiredService<DialectDslFrontendModule>();
        var compiler = provider.GetRequiredService<DialectDslCompiler>();
        var factory = provider.GetRequiredService<IDialectDslRegistryFactory>();

        Assert.Multiple(() =>
        {
            Assert.That(registry.DirectiveFeatures, Is.Empty);
            Assert.That(registry.DocumentRules, Is.Empty);
            Assert.That(frontendModule.Registry, Is.SameAs(registry));
            Assert.That(factory.CreateRegistry().DirectiveFeatures, Is.Empty);
            Assert.That(() => compiler.Compile("dialect Demo\n"), Throws.Nothing);
        });
    }

    [Test]
    public void AddDialectDslBuiltIns_ShouldRegisterExpectedBuiltInFeaturesAndRules()
    {
        var services = new ServiceCollection();
        services.AddDialectDsl();
        services.AddDialectDslBuiltIns();

        using var provider = services.BuildServiceProvider();
        var features = provider.GetServices<IDialectDirectiveFeature>().ToList();
        var rules = provider.GetServices<IDialectDocumentValidationRule>().ToList();
        var registry = provider.GetRequiredService<DialectDslRegistry>();

        Assert.Multiple(() =>
        {
            Assert.That(features.Select(x => x.Keyword), Is.EquivalentTo(DialectDslTestSupport.ExpectedBuiltInKeywords));
            Assert.That(rules.Select(x => x.GetType().Name), Is.EqualTo(new[] { "UseExcludeConflictDocumentValidationRule" }));
            Assert.That(registry.DirectiveFeatures.Select(x => x.Keyword), Is.EqualTo(DialectDslTestSupport.ExpectedBuiltInKeywords));
        });
    }

    [Test]
    public void AddDialectDslDefaultComposition_ShouldResolveCompilerRegistryAndFrontendModule_WithSharedRegistryInstance()
    {
        using var provider = DialectDslTestComposition.CreateProvider();

        var registry = provider.GetRequiredService<DialectDslRegistry>();
        var frontendModule = provider.GetRequiredService<DialectDslFrontendModule>();
        var compiler = provider.GetRequiredService<DialectDslCompiler>();
        var frontendCoreModules = provider.GetServices<IFrontendCoreModule>().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(registry.DirectiveFeatures.Select(x => x.Keyword), Is.EqualTo(DialectDslTestSupport.ExpectedBuiltInKeywords));
            Assert.That(registry.DocumentRules, Has.Count.EqualTo(1));
            Assert.That(frontendModule.Registry, Is.SameAs(registry));
            Assert.That(frontendCoreModules, Has.Member(frontendModule));
            Assert.That(compiler.Compile("dialect Demo\ncapability sandbox\n").CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[] { "sandbox" }));
        });
    }

    [TestCaseSource(typeof(DialectDslTestSupport), nameof(DialectDslTestSupport.RepresentativeSources))]
    public void StandaloneCompiler_ShouldMatchDiComposedCompiler_ForRepresentativeBuiltInInputs(string source)
    {
        var standalone = new DialectDslCompiler().Compile(source);
        var di = DialectDslTestComposition.CreateCompiler().Compile(source);

        DialectDslTestSupport.AssertSlicesEquivalent(standalone, di);
    }

    [TestCaseSource(typeof(DialectDslTestSupport), nameof(DialectDslTestSupport.RepresentativeSources))]
    public void StandaloneCompiler_ShouldMatchExplicitFrontendPipeline_ForRepresentativeBuiltInInputs(string source)
    {
        var standalone = new DialectDslCompiler().Compile(source);
        var viaFrontendModule = DialectDslTestSupport.CompileWithFrontendModule(DialectDslTestComposition.CreateFrontendModule(), source);

        DialectDslTestSupport.AssertSlicesEquivalent(standalone, viaFrontendModule);
    }

    [Test]
    public void StandaloneAndDiComposition_ShouldRejectUseExcludeConflicts_WithEquivalentErrorMessages()
    {
        const string source = "dialect Demo\nuse Arithmetic\nexclude Arithmetic\n";

        var standalone = Assert.Throws<ParserException>(() => new DialectDslCompiler().Compile(source));
        var di = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile(source));

        Assert.Multiple(() =>
        {
            DialectDslTestSupport.AssertParserExceptionContains(standalone!, "use", "exclude", "Arithmetic");
            DialectDslTestSupport.AssertParserExceptionContains(di!, "use", "exclude", "Arithmetic");
            Assert.That(di!.Message, Is.EqualTo(standalone!.Message));
        });
    }

    [Test]
    public void StandaloneAndDiComposition_ShouldRejectDuplicateSingletonSecurityDirective_WithEquivalentErrorMessages()
    {
        const string source = "dialect Demo\nsecurity trusted\nsecurity restricted\n";

        var standalone = Assert.Throws<ParserException>(() => new DialectDslCompiler().Compile(source));
        var di = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile(source));

        Assert.Multiple(() =>
        {
            DialectDslTestSupport.AssertParserExceptionContains(standalone!, "Security directive can only be declared once");
            DialectDslTestSupport.AssertParserExceptionContains(di!, "Security directive can only be declared once");
            Assert.That(di!.Message, Is.EqualTo(standalone!.Message));
        });
    }

    [Test]
    public void DefaultComposition_ShouldExposeSameBuiltInDescriptors_InDiRegistryAndFrontendModuleRegistry()
    {
        using var provider = DialectDslTestComposition.CreateProvider();
        var registry = provider.GetRequiredService<DialectDslRegistry>();
        var module = provider.GetRequiredService<DialectDslFrontendModule>();

        var fromRegistry = DialectDirectiveDescriptors.CreateOrdered(registry)
            .Select(x => (x.Id, x.Keyword, x.ParserOrder.Slot, x.ParserOrder.Sequence, x.IsSingleton))
            .ToArray();
        var fromModule = DialectDirectiveDescriptors.CreateOrdered(module.Registry)
            .Select(x => (x.Id, x.Keyword, x.ParserOrder.Slot, x.ParserOrder.Sequence, x.IsSingleton))
            .ToArray();

        Assert.That(fromModule, Is.EqualTo(fromRegistry));
    }
}