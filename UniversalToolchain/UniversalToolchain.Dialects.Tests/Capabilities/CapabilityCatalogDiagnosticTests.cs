using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

/// <summary>
///     Pins the two capability-catalog diagnostics that are raised in production and had no test.
/// </summary>
/// <remarks>
///     <para>
///         Both are reachable through the public <see cref="SelectedCapabilityCatalogBuilder" />, which is the
///         real production path: <c>WistDirectRuntimeComponents</c> builds the catalog from the types the plan
///         selected. That makes them ordinary untested branches rather than dead code.
///     </para>
///     <para>
///         <c>UTC-CAP-001</c> has three independent triggers, and they are asserted separately on purpose. A
///         single "some provider is rejected" test would not distinguish them: the discovery stage and the
///         activation stage both reject the same type, so a test that passes one guard's removal proves
///         nothing about the other. Writing these found that
///         <c>!constructor.IsPublic</c> in the activation guard was unreachable:
///         <c>GetConstructor(Type.EmptyTypes)</c> binds <c>Public|Instance</c> only, so a non-null result is
///         always public - and the condition was deleted rather than dressed up.
///     </para>
/// </remarks>
[TestFixture]
public sealed class CapabilityCatalogDiagnosticTests
{
    private static readonly LanguageFeatureId DuplicatedFeature = new("capability.duplicate.feature");

    [Test]
    public void ProviderTypeThatImplementsNoKnownInterface_IsReportedByDiscovery()
    {
        // Reported by CapabilityProviderTypeResolver.Resolve, before any activation is attempted.
        var discovery = new CapabilityProviderTypeResolver().Resolve([typeof(ModuleDeclaringNonProvider)]);

        Assert.Multiple(() =>
        {
            Assert.That(
                discovery.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(ToolchainDiagnosticCodes.CapabilityProviderInvalid));
            Assert.That(discovery.ProviderDescriptors, Is.Empty);

            var catalog = new SelectedCapabilityCatalogBuilder().Build([typeof(ModuleDeclaringNonProvider)]);
            Assert.That(catalog.Providers, Is.Empty);
        });
    }

    [Test]
    public void ProviderTypeWithoutPublicParameterlessConstructor_IsReportedByActivation()
    {
        // The type *is* a known provider interface, so discovery accepts it and the descriptor reaches the
        // factory. This is the activation guard, not the discovery guard.
        var discovery = new CapabilityProviderTypeResolver().Resolve([typeof(ModuleDeclaringCtorOnlyProvider)]);

        Assert.Multiple(() =>
        {
            Assert.That(discovery.ProviderDescriptors, Has.Count.EqualTo(1),
                "discovery must accept a known provider interface; otherwise this test would not reach activation");

            var catalog = new SelectedCapabilityCatalogBuilder().Build([typeof(ModuleDeclaringCtorOnlyProvider)]);
            var reported = catalog.Diagnostics.Single(
                static diagnostic => diagnostic.Code == ToolchainDiagnosticCodes.CapabilityProviderInvalid);
            Assert.That(catalog.Providers, Is.Empty);
            // The abstract branch and the ctor branch share a code, and this fixture must prove it reached
            // the ctor branch rather than the abstract one: the remedy - add a parameterless ctor - differs.
            Assert.That(reported.Message, Does.Contain("public parameterless constructor"));
        });
    }

    [Test]
    public void AbstractProviderTypeWithAPublicConstructor_IsReportedByActivation()
    {
        // The one trigger that `GetConstructor` cannot catch: an abstract type may declare a public
        // parameterless constructor, so the constructor lookup succeeds and only IsAbstract rejects it.
        var discovery = new CapabilityProviderTypeResolver().Resolve([typeof(ModuleDeclaringAbstractProvider)]);

        Assert.Multiple(() =>
        {
            Assert.That(discovery.ProviderDescriptors, Has.Count.EqualTo(1),
                "an abstract provider still has a public constructor, so discovery cannot reject it");

            var catalog = new SelectedCapabilityCatalogBuilder().Build([typeof(ModuleDeclaringAbstractProvider)]);
            var reported = catalog.Diagnostics.Single(
                static diagnostic => diagnostic.Code == ToolchainDiagnosticCodes.CapabilityProviderInvalid);
            Assert.That(catalog.Providers, Is.Empty);
            // The message is asserted, not just the code, because without the dedicated IsAbstract branch the
            // same code arrives from the activation catch-all. An abstract type that declares a public
            // constructor being told it "must declare a public parameterless constructor" is a wrong diagnosis
            // with a correct code, which is the kind of thing only a message assertion catches.
            Assert.That(reported.Message, Does.Contain("abstract"));
        });
    }

    [Test]
    public void TwoProvidersDeclaringTheSameFeatureId_ReportsDuplicateLanguageFeature()
    {
        var catalog = new SelectedCapabilityCatalogBuilder().Build(
            [typeof(FirstDuplicateFeatureModule), typeof(SecondDuplicateFeatureModule)]);

        Assert.Multiple(() =>
        {
            Assert.That(
                catalog.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(ToolchainDiagnosticCodes.DuplicateLanguageFeature));
            // Exactly one owner survives: the duplicate is reported *and* excluded, so the catalog never
            // contains a feature id with two owners. Reporting without excluding would leave the ambiguity in
            // place for every consumer downstream.
            Assert.That(
                catalog.LanguageFeatures.Count(static feature => feature.FeatureId == DuplicatedFeature),
                Is.EqualTo(1));
        });
    }

    private sealed class NotAProvider;

    [DialectCapabilityProvider(typeof(NotAProvider))]
    private sealed class ModuleDeclaringNonProvider;

    private sealed class CtorOnlyProvider : ILanguageFeatureDescriptorProvider
    {
        public CtorOnlyProvider(string unused) => _ = unused;

        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() => [];
    }

    [DialectCapabilityProvider(typeof(CtorOnlyProvider))]
    private sealed class ModuleDeclaringCtorOnlyProvider;

    private abstract class AbstractProvider : ILanguageFeatureDescriptorProvider
    {
        // Explicitly public: the class is private-nested, so its implicit constructor would be private and
        // GetConstructor(Type.EmptyTypes) would return null - the fixture would then exercise the
        // "no constructor" branch instead of the abstract-type branch.
        public AbstractProvider()
        {
        }

        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() => [];
    }

    [DialectCapabilityProvider(typeof(AbstractProvider))]
    private sealed class ModuleDeclaringAbstractProvider;

    private sealed class DuplicateFeatureProviderA : ILanguageFeatureDescriptorProvider
    {
        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() => [Descriptor];
    }

    private sealed class DuplicateFeatureProviderB : ILanguageFeatureDescriptorProvider
    {
        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() => [Descriptor];
    }

    [DialectCapabilityProvider(typeof(DuplicateFeatureProviderA))]
    private sealed class FirstDuplicateFeatureModule;

    [DialectCapabilityProvider(typeof(DuplicateFeatureProviderB))]
    private sealed class SecondDuplicateFeatureModule;

    private static LanguageFeatureDescriptor Descriptor => new(
        DuplicatedFeature,
        "Duplicate feature",
        LanguageFeatureKind.Syntax,
        [],
        [],
        [],
        [],
        "Declared twice on purpose.");

}
