using UniversalToolchain.Dialects.Integration;
using Wistc;

namespace UniversalToolchain.Dialects.Tests.Wist;

public sealed class WistCliDefaultAndListingTests
{
    [Test]
    public void WistCliCustomizationRequest_FromOptionsWithoutOverrides_DoesNotRequestCustomization()
    {
        var request = WistCliCustomizationRequest.FromOptions(new CommonOptions());

        Assert.That(request.HasCustomization, Is.False);
    }

    [Test]
    public void WistCliCustomizationRequest_FromOptionsWithIncludeModules_RequestsCustomization()
    {
        var request = WistCliCustomizationRequest.FromOptions(new CommonOptions
        {
            IncludeModules = ["  ExtraModule  "]
        });

        Assert.Multiple(() =>
        {
            Assert.That(request.HasCustomization, Is.True);
            Assert.That(request.IncludeModules, Is.EqualTo(new[] { "ExtraModule" }));
        });
    }

    [Test]
    public void WistCliCustomizationRequest_FromOptionsWithExcludeModules_RequestsCustomization()
    {
        var request = WistCliCustomizationRequest.FromOptions(new CommonOptions
        {
            ExcludeModules = ["  CSharpInterop  "]
        });

        Assert.Multiple(() =>
        {
            Assert.That(request.HasCustomization, Is.True);
            Assert.That(request.ExcludeModules, Is.EqualTo(new[] { "CSharpInterop" }));
        });
    }

    [Test]
    public void WistCliCustomizationRequest_FromOptionsWithNativeMath_RequestsCustomization()
    {
        var request = WistCliCustomizationRequest.FromOptions(new CommonOptions
        {
            UseNativeMath = true
        });

        Assert.That(request.HasCustomization, Is.True);
    }

    [Test]
    public void RuntimeListing_UsesRuntimeComponentCatalog()
    {
        var output = WistCliRuntimeListingFormatter.Format(new StaticCatalog(
            modules: [Entry(RuntimeComponentKind.FrontendModule, "Arithmetic", [], "frontend.arithmetic", "ArithmeticModule")],
            optimizers: [],
            backends: [Entry(RuntimeComponentKind.Backend, "cil", ["compiler"], "backend.cil", "UniversalToolchain.Dialects.Wist")]));

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Modules:"));
            Assert.That(output, Does.Contain("Arithmetic | id: frontend.arithmetic | assembly: ArithmeticModule"));
            Assert.That(output, Does.Contain("cil | aliases: compiler | id: backend.cil | assembly: UniversalToolchain.Dialects.Wist"));
        });
    }

    [Test]
    public void RuntimeListing_Output_IsDeterministicallyOrdered()
    {
        var output = WistCliRuntimeListingFormatter.Format(new StaticCatalog(
            modules:
            [
                Entry(RuntimeComponentKind.FrontendModule, "Alpha", [], "frontend.alpha", "AlphaModule"),
                Entry(RuntimeComponentKind.FrontendModule, "Beta", [], "frontend.beta", "BetaModule")
            ],
            optimizers: [],
            backends: []));

        Assert.That(output.IndexOf("  Alpha", StringComparison.Ordinal), Is.LessThan(output.IndexOf("  Beta", StringComparison.Ordinal)));
    }

    private static RuntimeComponentManifestEntry Entry(
        RuntimeComponentKind kind,
        string canonicalAlias,
        IReadOnlyList<string> aliases,
        string componentId,
        string assemblySimpleName)
        => new(kind, canonicalAlias, aliases, new RuntimeComponentId(componentId), assemblySimpleName);

    private sealed class StaticCatalog(
        IReadOnlyList<RuntimeComponentManifestEntry> modules,
        IReadOnlyList<RuntimeComponentManifestEntry> optimizers,
        IReadOnlyList<RuntimeComponentManifestEntry> backends) : IRuntimeComponentCatalog
    {
        public bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry)
            => TryResolve(modules, alias, out entry);

        public bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry)
            => TryResolve(optimizers, alias, out entry);

        public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry)
            => TryResolve(backends, alias, out entry);

        public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => modules;

        public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => optimizers;

        public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => backends;

        private static bool TryResolve(
            IReadOnlyList<RuntimeComponentManifestEntry> entries,
            string alias,
            out RuntimeComponentManifestEntry? entry)
        {
            entry = entries.FirstOrDefault(x => x.AllAliases.Contains(alias, StringComparer.Ordinal));
            return entry != null;
        }
    }
}
