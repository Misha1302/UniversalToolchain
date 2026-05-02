using Wistc;

namespace UniversalToolchain.Dialects.Tests.Wist;

public sealed class WistCliDefaultAndListingTests
{
    [Test]
    public void WistCliCustomizationRequest_FromOptions_DoesNotRequestRawDialectTextMutation()
    {
        var request = WistCliCustomizationRequest.FromOptions(new CommonOptions());

        Assert.That(request.HasCustomization, Is.False);
    }

    [Test]
    public void RuntimeListing_UsesRuntimeComponentCatalog()
    {
        var output = WistCliRuntimeListingFormatter.Format(new StaticCatalog(
            [Entry(RuntimeComponentKind.FrontendModule, "Arithmetic", [], "frontend.arithmetic", "ArithmeticModule")],
            [],
            [Entry(RuntimeComponentKind.Backend, "cil", ["compiler"], "backend.cil", "UniversalToolchain.Dialects.Wist")]));

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
            [
                Entry(RuntimeComponentKind.FrontendModule, "Alpha", [], "frontend.alpha", "AlphaModule"),
                Entry(RuntimeComponentKind.FrontendModule, "Beta", [], "frontend.beta", "BetaModule")
            ],
            [],
            []));

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