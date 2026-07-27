using System.Reflection;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

public class ReflectionCapabilityDiscoveryTests
{
    private static readonly FunctionTypeDescriptor _numberType = new("number");
    private static int _moduleInstantiationCount;

    [SetUp]
    public void SetUp()
    {
        _moduleInstantiationCount = 0;
    }

    [Test]
    public void Discovery_ReadsProviderAttribute_FromComponentType()
    {
        var resolver = new CapabilityProviderTypeResolver();

        var result = resolver.Resolve([typeof(FakeFunctionModuleImpl)]);

        Assert.That(result.ProviderDescriptors.Select(static x => x.ProviderType), Is.EqualTo(new[] { typeof(FakeFunctionCapabilityProvider) }));
    }

    [Test]
    public void Discovery_DoesNotInstantiateRuntimeModule()
    {
        var resolver = new CapabilityProviderTypeResolver();

        _ = resolver.Resolve([typeof(ConstructorTrackedModuleImpl)]);

        Assert.That(_moduleInstantiationCount, Is.Zero);
    }

    [Test]
    public void Discovery_RejectsProviderWithNoKnownInterface()
    {
        var resolver = new CapabilityProviderTypeResolver();

        var result = resolver.Resolve([typeof(InvalidProviderModuleImpl)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.ProviderDescriptors, Is.Empty);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { ToolchainDiagnosticCodes.CapabilityProviderInvalid }));
        });
    }

    [Test]
    public void Discovery_DeterministicOrder()
    {
        var resolver = new CapabilityProviderTypeResolver();

        var result = resolver.Resolve([typeof(ZetaModuleImpl), typeof(AlphaModuleImpl)]);

        Assert.That(
            result.ProviderDescriptors.Select(static x => $"{x.RuntimeComponentImplementationType.Name}:{x.ProviderType.Name}"),
            Is.EqualTo(new[]
            {
                "AlphaModuleImpl:AlphaSecondaryCapabilityProvider",
                "AlphaModuleImpl:ZetaPrimaryCapabilityProvider",
                "ZetaModuleImpl:FakeFunctionCapabilityProvider"
            }));
    }

    [Test]
    public void KnownCatalog_IncludesAllManifestComponentProviders()
    {
        var builder = new KnownCapabilityCatalogBuilder(new StaticRuntimeComponentTypeLoader(new Dictionary<RuntimeComponentId, Type>
        {
            [new RuntimeComponentId("fake-module-id")] = typeof(FakeFunctionModuleImpl),
            [new RuntimeComponentId("known-only-module-id")] = typeof(KnownOnlyModuleImpl)
        }));

        var catalog = builder.Build(new StaticRuntimeComponentCatalog(
            [CreateEntry("fake-module", "fake-module-id"), CreateEntry("known-only-module", "known-only-module-id")],
            [],
            []));

        Assert.That(catalog.Providers.Select(static x => x.ProviderType), Is.EqualTo(new[]
        {
            typeof(FakeFunctionCapabilityProvider),
            typeof(KnownOnlyCapabilityProvider)
        }));
    }

    [Test]
    public void SelectedCatalog_IncludesOnlySelectedComponentProviders()
    {
        var builder = new SelectedCapabilityCatalogBuilder(new StaticRuntimeComponentTypeLoader(new Dictionary<RuntimeComponentId, Type>
        {
            [new RuntimeComponentId("fake-module-id")] = typeof(FakeFunctionModuleImpl),
            [new RuntimeComponentId("known-only-module-id")] = typeof(KnownOnlyModuleImpl)
        }));
        var selectedPlan = new SelectedRuntimePlan(
            [CreateEntry("fake-module", "fake-module-id")],
            [],
            [],
            []);

        var catalog = builder.Build(selectedPlan);

        Assert.That(catalog.Providers.Select(static x => x.ProviderType), Is.EqualTo(new[] { typeof(FakeFunctionCapabilityProvider) }));
    }

    [Test]
    public void SelectedCatalog_DuplicateLanguageFeatureId_ReturnsDeterministicError()
    {
        var catalog = new SelectedCapabilityCatalogBuilder().Build(
            [typeof(DuplicateAlphaModuleImpl), typeof(DuplicateZetaModuleImpl)]);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.LanguageFeatures.Select(static x => x.FeatureId.Value),
                Is.EqualTo(new[] { "duplicate-feature" }));
            Assert.That(catalog.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(catalog.Diagnostics[0].Code, Is.EqualTo(ToolchainDiagnosticCodes.DuplicateLanguageFeature));
            Assert.That(catalog.Diagnostics[0].Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Error));
            Assert.That(catalog.TryGetOwningProvider(new LanguageFeatureId("duplicate-feature"), out var owner), Is.True);
            Assert.That(owner.ProviderType, Is.EqualTo(typeof(DuplicateAlphaCapabilityProvider)));
        });
    }

    [Test]
    public void FeatureExplanation_ReportsSelectedFeatureAvailable()
    {
        var selectedPlan = new SelectedRuntimePlan(
            [CreateEntry("fake-module", "fake-module-id")],
            [],
            [CreateEntry("interpreter", "interpreter-id", RuntimeComponentKind.Backend)],
            []);
        var knownCatalog = new KnownCapabilityCatalogBuilder().Build([typeof(FakeFunctionModuleImpl), typeof(KnownOnlyModuleImpl)]);
        var selectedCatalog = new SelectedCapabilityCatalogBuilder().Build([typeof(FakeFunctionModuleImpl)]);

        var explanation = DialectFeatureExplanationProjector.Project(knownCatalog, selectedCatalog, selectedPlan, "FakeDialect");

        Assert.That(explanation.AvailableFeatures.Select(static x => x.FeatureId.Value), Is.EqualTo(new[] { "fake-functions" }));
    }

    [Test]
    public void FeatureExplanation_ReportsKnownButUnselectedFeatureUnavailable()
    {
        var selectedPlan = new SelectedRuntimePlan(
            [CreateEntry("fake-module", "fake-module-id")],
            [],
            [CreateEntry("interpreter", "interpreter-id", RuntimeComponentKind.Backend)],
            []);
        var knownCatalog = new KnownCapabilityCatalogBuilder().Build([typeof(FakeFunctionModuleImpl), typeof(KnownOnlyModuleImpl)]);
        var selectedCatalog = new SelectedCapabilityCatalogBuilder().Build([typeof(FakeFunctionModuleImpl)]);

        var explanation = DialectFeatureExplanationProjector.Project(knownCatalog, selectedCatalog, selectedPlan, "FakeDialect");
        var unavailableFeature = explanation.UnavailableKnownFeatures.Single(static x => x.Feature.FeatureId.Value == "known-only");

        Assert.That(unavailableFeature.Reasons.Any(static x => x.Contains("not selected", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void FunctionCatalog_ResolvesFakeFunctionWithoutCentralResolverChanges()
    {
        var selectedPlan = new SelectedRuntimePlan(
            [CreateEntry("fake-module", "fake-module-id")],
            [],
            [CreateEntry("interpreter", "interpreter-id", RuntimeComponentKind.Backend)],
            []);
        var selectedCatalog = new SelectedCapabilityCatalogBuilder().Build([typeof(FakeFunctionModuleImpl)]);
        var functionCatalog = new BuiltinFunctionCatalog(selectedCatalog, selectedPlan);

        var resolution = functionCatalog.Resolve("fakeAdd", [_numberType, _numberType], "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(resolution.IsSuccess, Is.True);
            Assert.That(resolution.Descriptor?.Name, Is.EqualTo("fakeAdd"));
            Assert.That(resolution.RuntimeBinding?.Method, Is.EqualTo(typeof(FakeRuntimeMethods).GetMethod(nameof(FakeRuntimeMethods.FakeAdd), BindingFlags.Public | BindingFlags.Static)));
        });
    }

    [Test]
    public void FunctionCatalog_UnknownFunction_ReturnsDiagnostic()
    {
        var selectedPlan = new SelectedRuntimePlan(
            [CreateEntry("fake-module", "fake-module-id")],
            [],
            [CreateEntry("interpreter", "interpreter-id", RuntimeComponentKind.Backend)],
            []);
        var selectedCatalog = new SelectedCapabilityCatalogBuilder().Build([typeof(FakeFunctionModuleImpl)]);
        var functionCatalog = new BuiltinFunctionCatalog(selectedCatalog, selectedPlan);

        var resolution = functionCatalog.Resolve("missingFunction", [_numberType, _numberType], "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(resolution.IsSuccess, Is.False);
            Assert.That(resolution.Diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { ToolchainDiagnosticCodes.UnknownFunction }));
        });
    }

    [Test]
    public void Architecture_AddingFakeFunctionProvider_DoesNotRequireResolverChanges()
    {
        var selectedPlan = new SelectedRuntimePlan(
            [CreateEntry("fake-module", "fake-module-id")],
            [],
            [CreateEntry("interpreter", "interpreter-id", RuntimeComponentKind.Backend)],
            []);
        var knownCatalog = new KnownCapabilityCatalogBuilder().Build([typeof(FakeFunctionModuleImpl)]);
        var selectedCatalog = new SelectedCapabilityCatalogBuilder().Build([typeof(FakeFunctionModuleImpl)]);
        var functionCatalog = new BuiltinFunctionCatalog(selectedCatalog, selectedPlan);
        var explanation = DialectFeatureExplanationProjector.Project(knownCatalog, selectedCatalog, selectedPlan, "FakeDialect");

        var resolution = functionCatalog.Resolve("fakeAdd", [_numberType, _numberType], "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(resolution.IsSuccess, Is.True);
            Assert.That(explanation.AvailableFeatures.Select(static x => x.FeatureId.Value), Does.Contain("fake-functions"));
            Assert.That(explanation.AvailableFunctions.Select(static x => x.Name), Does.Contain("fakeAdd"));
        });
    }

    private static RuntimeComponentManifestEntry CreateEntry(string alias, string id, RuntimeComponentKind kind = RuntimeComponentKind.FrontendModule) =>
        new(kind, alias, [
        ], new RuntimeComponentId(id), "TestAssembly",
            new RuntimeComponentActivationInfo(new RuntimeTypeReference("TestAssembly", "Test.Activation.Type")));

    [DialectCapabilityProvider(typeof(FakeFunctionCapabilityProvider))]
    private sealed class FakeFunctionModuleImpl
    {
    }

    [DialectCapabilityProvider(typeof(FakeFunctionCapabilityProvider))]
    private sealed class ConstructorTrackedModuleImpl
    {
        public ConstructorTrackedModuleImpl()
        {
            _moduleInstantiationCount++;
        }
    }

    [DialectCapabilityProvider(typeof(NoKnownCapabilityProvider))]
    private sealed class InvalidProviderModuleImpl
    {
    }

    [DialectCapabilityProvider(typeof(ZetaPrimaryCapabilityProvider))]
    [DialectCapabilityProvider(typeof(AlphaSecondaryCapabilityProvider))]
    private sealed class AlphaModuleImpl
    {
    }

    [DialectCapabilityProvider(typeof(FakeFunctionCapabilityProvider))]
    private sealed class ZetaModuleImpl
    {
    }

    [DialectCapabilityProvider(typeof(KnownOnlyCapabilityProvider))]
    private sealed class KnownOnlyModuleImpl
    {
    }

    [DialectCapabilityProvider(typeof(DuplicateAlphaCapabilityProvider))]
    private sealed class DuplicateAlphaModuleImpl
    {
    }

    [DialectCapabilityProvider(typeof(DuplicateZetaCapabilityProvider))]
    private sealed class DuplicateZetaModuleImpl
    {
    }

    private sealed class FakeFunctionCapabilityProvider :
        ILanguageFeatureDescriptorProvider,
        IBuiltinFunctionDescriptorProvider,
        IBuiltinFunctionRuntimeBindingProvider
    {
        public IReadOnlyList<BuiltinFunctionDescriptor> GetFunctions() =>
        [
            new(
                "fakeAdd",
                new LanguageFeatureId("fake-functions"),
                [new FunctionParameterDescriptor("left", _numberType), new FunctionParameterDescriptor("right", _numberType)],
                _numberType,
                FunctionPurity.Pure,
                ["interpreter"])
        ];

        public IReadOnlyList<BuiltinFunctionRuntimeBinding> GetRuntimeBindings() =>
        [
            new(
                new BuiltinFunctionSignature("fakeAdd", [_numberType, _numberType]),
                _numberType,
                new LanguageFeatureId("fake-functions"),
                typeof(FakeRuntimeMethods).GetMethod(nameof(FakeRuntimeMethods.FakeAdd), BindingFlags.Public | BindingFlags.Static)!,
                ["interpreter"])
        ];

        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
        [
            new(
                new LanguageFeatureId("fake-functions"),
                "Fake Functions",
                LanguageFeatureKind.FunctionSet,
                ["fake-module"],
                [],
                [new LanguageFeatureSymbolDescriptor("fakeAdd", LanguageFeatureSymbolKind.Function, "fakeAdd(number, number)", "Adds fake numbers.")],
                ["interpreter"],
                "Fake arithmetic functions.")
        ];
    }

    private sealed class KnownOnlyCapabilityProvider : ILanguageFeatureDescriptorProvider
    {
        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
        [
            new(
                new LanguageFeatureId("known-only"),
                "Known Only",
                LanguageFeatureKind.Syntax,
                ["known-only-module"],
                [],
                [],
                ["interpreter"],
                "Feature used to prove known but unselected reporting.")
        ];
    }

    private sealed class DuplicateAlphaCapabilityProvider : ILanguageFeatureDescriptorProvider
    {
        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() => [CreateDuplicateFeature("Alpha")];
    }

    private sealed class DuplicateZetaCapabilityProvider : ILanguageFeatureDescriptorProvider
    {
        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() => [CreateDuplicateFeature("Zeta")];
    }

    private static LanguageFeatureDescriptor CreateDuplicateFeature(string displayName) =>
        new(
            new LanguageFeatureId("duplicate-feature"),
            displayName,
            LanguageFeatureKind.Syntax,
            [],
            [],
            [],
            [],
            "Duplicate-feature collision test.");

    private sealed class ZetaPrimaryCapabilityProvider : ILanguageFeatureDescriptorProvider
    {
        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() => [];
    }

    private sealed class AlphaSecondaryCapabilityProvider : ILanguageFeatureDescriptorProvider
    {
        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() => [];
    }

    private sealed class NoKnownCapabilityProvider
    {
    }

    private sealed class StaticRuntimeComponentCatalog(
        IReadOnlyList<RuntimeComponentManifestEntry> modules,
        IReadOnlyList<RuntimeComponentManifestEntry> optimizers,
        IReadOnlyList<RuntimeComponentManifestEntry> backends) : IRuntimeComponentCatalog
    {
        public bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry)
        {
            entry = modules.SingleOrDefault(x => x.CanonicalAlias == alias);
            return entry != null;
        }

        public bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry)
        {
            entry = optimizers.SingleOrDefault(x => x.CanonicalAlias == alias);
            return entry != null;
        }

        public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry)
        {
            entry = backends.SingleOrDefault(x => x.CanonicalAlias == alias);
            return entry != null;
        }

        public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => modules;

        public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => optimizers;

        public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => backends;
    }

    private sealed class StaticRuntimeComponentTypeLoader(IReadOnlyDictionary<RuntimeComponentId, Type> typesById) : IRuntimeComponentTypeLoader
    {
        public Type LoadType(RuntimeComponentManifestEntry entry) => typesById[entry.ComponentId];
    }

    private static class FakeRuntimeMethods
    {
        public static double FakeAdd(double left, double right) => left + right;
    }
}