using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Tests.Wist;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Features;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Features.Core;

namespace UniversalToolchain.Dialects.Tests.Features;

[TestFixture]
public sealed class DialectFeatureExplanationProjectorTests
{
    [Test]
    public void Project_MinimalArithmetic_DoesNotExposeCSharpInterop()
    {
        var explanation = ProjectShippedDialect(WistShippedDialectPresets.MinimalArithmetic);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.DialectName, Is.EqualTo("MinimalArithmetic"));
            Assert.That(explanation.AvailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Not.Contain("CSharpInterop"));
            Assert.That(explanation.UnavailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Contain("CSharpInterop"));
        });
    }

    [Test]
    public void Project_FullDefault_WhenInteropSelected_ExposesCSharpInterop()
    {
        var explanation = ProjectShippedDialect(WistShippedDialectPresets.Default);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.AvailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Contain("CSharpInterop"));
            Assert.That(explanation.AvailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Contain("ArithmeticExpressions"));
            Assert.That(explanation.UnavailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Not.Contain("CSharpInterop"));
        });
    }

    [Test]
    public void Project_RestrictedSandbox_DoesNotReturnCSharpInterop()
    {
        var explanation = ProjectShippedDialect(WistShippedDialectPresets.RestrictedSandbox);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.AvailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Not.Contain("CSharpInterop"));
            Assert.That(explanation.UnavailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Contain("CSharpInterop"));
            Assert.That(explanation.BackendSupport.Select(static x => x.BackendAlias), Is.EqualTo(new[] { "interpreter" }));
        });
    }

    [Test]
    public void Project_WhenRequiredAliasMissing_ReturnsUnavailableReason()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var projector = CreateProjector();
        var composition = workflow.ComposeText(
            "dialect MissingScopes\nuse Identifier,Variables,Whitespaces\nbackend interpreter",
            "missing-scopes");

        Assert.That(composition.IsSuccess, Is.True);

        var explanation = projector.Project(composition);
        var unavailableVariables = explanation.UnavailableFeatures.Single(static x => x.Descriptor.FeatureId.Value == "Variables");

        Assert.That(
            unavailableVariables.Reasons,
            Does.Contain("Required feature 'Scopes' is not available."));
    }

    [Test]
    public void Project_RepeatedCalls_ReturnEquivalentExplanation()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var projector = CreateProjector();
        var dialectFile = new WistShippedDialectFileResolver().Resolve(WistShippedDialectPresets.PricingRestricted);
        var composition = workflow.ComposeFile(dialectFile);

        Assert.That(composition.IsSuccess, Is.True);

        var first = projector.Project(composition);
        var second = projector.Project(composition);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Project_DoesNotCreateBackendRegistrationSideEffects()
    {
        var registrar = new CountingRegistrar("interpreter");
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddSingleton<IDialectBackendRuntimeRegistrar>(registrar);

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            "dialect ComposeOnly\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter",
            "compose-only");

        Assert.That(composition.IsSuccess, Is.True);

        var explanation = CreateProjector().Project(composition);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.AvailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Contain("ArithmeticExpressions"));
            Assert.That(registrar.RegisterRuntimeCallCount, Is.EqualTo(0));
        });
    }

    private static DialectFeatureExplanation ProjectShippedDialect(WistShippedDialectPreset preset)
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialectFile = new WistShippedDialectFileResolver().Resolve(preset);
        var composition = workflow.ComposeFile(dialectFile);

        Assert.That(composition.IsSuccess, Is.True);

        return CreateProjector().Project(composition);
    }

    private static DialectFeatureExplanationProjector CreateProjector()
    {
        ILanguageFeatureCatalog catalog = new WistLanguageFeatureCatalog();
        return new DialectFeatureExplanationProjector(catalog);
    }

    private sealed class CountingRegistrar(string backendId) : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new(backendId);

        public IReadOnlyList<string> SupportedIntrinsics => [];

        public int RegisterRuntimeCallCount { get; private set; }

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
            RegisterRuntimeCallCount++;
            services.AddSingleton(typeof(object), new object());
        }
    }
}
