using CommonExceptions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend.Composition;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDslExtensibilityTests
{
    [Test]
    public void CustomProvider_ShouldRegisterDirectiveWithoutEditingDefaultPlumbing()
    {
        var compiler = DialectDslTestComposition.CreateCompiler(services => services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>());

        var slice = compiler.Compile("dialect Demo\nalias math arithmetic\nuse arithmetic\n");

        Assert.Multiple(() =>
        {
            Assert.That(slice.UseModules, Is.EqualTo(new[] { "arithmetic" }));
            Assert.That(slice.CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[] { "alias:math->arithmetic" }));
        });
    }

    [Test]
    public void CustomProviders_ShouldComposeTogetherAndCoexistWithBuiltIns()
    {
        var compiler = DialectDslTestComposition.CreateCompiler(services =>
        {
            services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>();
            services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider2>();
        });

        var slice = compiler.Compile("dialect Demo\nalias math arithmetic\nalias-2 io input\ncapability sandbox\n");

        Assert.That(slice.CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[]
        {
            "alias:math->arithmetic",
            "alias-2:io->input",
            "sandbox"
        }));
    }

    [Test]
    public void DirectFeatureRegistration_ShouldSupportCustomDirectiveOutsideProviderPath()
    {
        var compiler = DialectDslTestComposition.CreateCompiler(services => services.AddDialectDirectiveFeature<DirectAliasDirectiveFeature>());

        var slice = compiler.Compile("dialect Demo\ndirect-alias src dst\n");

        Assert.That(slice.CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[] { "direct-alias:src->dst" }));
    }

    [Test]
    public void ProviderRegistrationOrder_ShouldNotAffectFeatureAvailabilityOrFinalOrdering()
    {
        using var firstProvider = DialectDslTestComposition.CreateProvider(services =>
        {
            services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider2>();
            services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>();
        });
        using var secondProvider = DialectDslTestComposition.CreateProvider(services =>
        {
            services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>();
            services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider2>();
        });

        var firstRegistry = firstProvider.GetRequiredService<DialectDslRegistry>();
        var secondRegistry = secondProvider.GetRequiredService<DialectDslRegistry>();

        Assert.That(firstRegistry.DirectiveFeatures.Select(x => (x.Keyword, x.Id, x.ParserOrder.Sequence)),
            Is.EqualTo(secondRegistry.DirectiveFeatures.Select(x => (x.Keyword, x.Id, x.ParserOrder.Sequence))));
    }

    [Test]
    public void CustomDirective_ShouldParticipateInSemanticValidation_AndRejectInvalidPayloads()
    {
        var compiler = DialectDslTestComposition.CreateCompiler(services => services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>());

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect Demo\nalias same same\n"));

        DialectDslTestSupport.AssertParserExceptionContains(ex!, "Alias source and target must differ");
    }

    [Test]
    public void CustomDirective_ShouldParticipateInRegistryComposition_ThroughDiFactory()
    {
        using var provider = DialectDslTestComposition.CreateProvider(services => services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>());

        var registry = provider.GetRequiredService<DialectDslRegistry>();
        var factory = provider.GetRequiredService<IDialectDslRegistryFactory>();

        Assert.Multiple(() =>
        {
            Assert.That(registry.DirectiveFeatures.Select(x => x.Keyword), Does.Contain("alias"));
            Assert.That(factory.CreateRegistry().DirectiveFeatures.Select(x => x.Keyword), Does.Contain("alias"));
        });
    }

    [Test]
    public void DuplicateCustomKeywords_ShouldFailFast_DuringRegistryConstruction()
    {
        var services = new ServiceCollection();
        services.AddDialectDsl();
        services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>();
        services.AddDialectDirectiveFeature<DuplicateAliasDirectiveFeature>();

        using var provider = services.BuildServiceProvider();
        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<DialectDslRegistry>());

        Assert.That(ex!.Message, Does.Contain("keyword").And.Contain("alias"));
    }
}