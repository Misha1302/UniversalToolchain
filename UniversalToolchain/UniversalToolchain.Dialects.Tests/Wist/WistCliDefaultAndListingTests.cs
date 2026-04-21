using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist.Presets;
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
    public void WistCliCustomizedDialectBuilder_Build_EmitsCustomizationOnlyDialect()
    {
        var request = new WistCliCustomizationRequest(
            true,
            ["ExtraModule"],
            ["CSharpInterop"]);

        var dialectText = new WistCliCustomizedDialectBuilder().Build(request);

        Assert.Multiple(() =>
        {
            Assert.That(dialectText, Does.Contain("dialect CliCustomized"));
            Assert.That(dialectText, Does.Contain("NativeTypes"));
            Assert.That(dialectText, Does.Contain("ExtraModule"));
            Assert.That(dialectText, Does.Not.Contain("CSharpInterop"));
            Assert.That(dialectText, Does.Contain("backend cil,interpreter"));
        });
    }

    [Test]
    public void RuntimeListing_UsesRuntimeComponentCatalog()
    {
        var output = WistCliRuntimeListingFormatter.Format(new StaticCatalog(
            modules: [Entry(RuntimeComponentKind.FrontendModule, "Arithmetic", [], "frontend.arithmetic", "ArithmeticModule")],
            optimizers: [Entry(RuntimeComponentKind.Optimizer, "LocalVariablesOptimization", [], "optimizer.localvariables", "LocalVariablesOptimizerModule")],
            backends: [Entry(RuntimeComponentKind.Backend, "cil", ["compiler"], "backend.cil", "UniversalToolchain.Dialects.Wist")]));

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Modules:"));
            Assert.That(output, Does.Contain("Arithmetic | id: frontend.arithmetic | assembly: ArithmeticModule"));
            Assert.That(output, Does.Contain("cil | aliases: compiler | id: backend.cil | assembly: UniversalToolchain.Dialects.Wist"));
            Assert.That(output, Does.Not.Contain("TypesFinder"));
            Assert.That(output, Does.Not.Contain("AutoRegisterServiceAttribute"));
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

    [Test]
    public void RuntimeListing_DoesNotDependOnTypesFinder()
    {
        var source = File.ReadAllText(Path.Combine(GetRepoRoot(), "UniversalToolchain", "Wistc", "Program.cs"))
            + File.ReadAllText(Path.Combine(GetRepoRoot(), "UniversalToolchain", "Wistc", "WistCliRuntimeListingFormatter.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Not.Contain("TypesFinder"));
            Assert.That(source, Does.Not.Contain("AutoRegisterServiceAttribute"));
            Assert.That(source, Does.Not.Contain("GetTypes()"));
        });
    }

    [Test]
    public void FacadeDefault_And_CliDefault_UseSameShippedPreset()
    {
        var source = File.ReadAllText(Path.Combine(GetRepoRoot(), "UniversalToolchain", "Wistc", "Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(WistShippedDialectPresets.Default, Is.SameAs(WistShippedDialectPresets.FullDefault));
            Assert.That(source, Does.Contain("CreateHostFromPreset(workflow, WistShippedDialectPresets.Default)"));
        });
    }

    private static RuntimeComponentManifestEntry Entry(
        RuntimeComponentKind kind,
        string canonicalAlias,
        IReadOnlyList<string> aliases,
        string componentId,
        string assemblySimpleName)
        => new(kind, canonicalAlias, aliases, new RuntimeComponentId(componentId), assemblySimpleName);

    private static string GetRepoRoot()
        => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

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
