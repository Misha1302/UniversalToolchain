using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeCatalog;

public class SelectedRuntimePlanResolverContractTests
{
    [Test]
    public void Resolve_ShouldPreserveDeclaredModuleOrder()
    {
        var resolver = CreateResolver();
        var plan = BuildPlan(["Numbers", "Arithmetic"], [new DialectBackendId("interpreter")]);

        var selected = resolver.Resolve(plan);

        Assert.That(selected.OrderedModules.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "Numbers", "Arithmetic" }));
    }

    [Test]
    public void Resolve_ShouldSortBackendsDeterministically()
    {
        var resolver = CreateResolver();
        var plan = BuildPlan(["Arithmetic"], [new DialectBackendId("interpreter"), new DialectBackendId("compiler")]);

        var selected = resolver.Resolve(plan);

        Assert.That(selected.EnabledBackends.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "compiler", "interpreter" }));
    }

    [Test]
    public void Resolve_ShouldIgnoreDuplicateBackendSelection()
    {
        var resolver = CreateResolver();
        var plan = BuildPlan(["Arithmetic"], [new DialectBackendId("interpreter"), new DialectBackendId("interpreter")]);

        var selected = resolver.Resolve(plan);

        Assert.That(selected.EnabledBackends.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "interpreter" }));
    }

    [Test]
    public void Resolve_ShouldFilterOptimizersBySelectedBackendTargets()
    {
        var resolver = CreateResolver();
        var plan = BuildPlan(
            ["Arithmetic"],
            [new DialectBackendId("interpreter")],
            [
                new OptimizerBuildDirective("CommonOpt", true, DialectBackendSelector.Any),
                new OptimizerBuildDirective("CompilerOnlyOpt", true, DialectBackendSelector.For(new DialectBackendId("compiler"))),
                new OptimizerBuildDirective("InterpreterOnlyOpt", true, DialectBackendSelector.For(new DialectBackendId("interpreter")))
            ]);

        var selected = resolver.Resolve(plan);

        Assert.That(selected.EnabledOptimizers.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "CommonOpt", "InterpreterOnlyOpt" }));
    }

    [Test]
    public void Resolve_ShouldAddR001Diagnostic_ForMissingModule()
    {
        var resolver = CreateResolver();
        var selected = resolver.Resolve(BuildPlan(["MissingModule"], [new DialectBackendId("interpreter")]));

        Assert.That(selected.Diagnostics.Single(static x => x.Code == "R001").Message, Does.Contain("MissingModule"));
    }

    [Test]
    public void Resolve_ShouldAddR002Diagnostic_ForMissingBackend()
    {
        var resolver = CreateResolver();
        var selected = resolver.Resolve(BuildPlan(["Arithmetic"], [new DialectBackendId("missing-backend")]));

        Assert.That(selected.Diagnostics.Single(static x => x.Code == "R002").Message, Does.Contain("missing-backend"));
    }

    [Test]
    public void Resolve_ShouldAddR003Diagnostic_ForMissingOptimizer()
    {
        var resolver = CreateResolver();
        var selected = resolver.Resolve(BuildPlan(
            ["Arithmetic"],
            [new DialectBackendId("interpreter")],
            [new OptimizerBuildDirective("missing-opt", true, DialectBackendSelector.Any)]));

        Assert.That(selected.Diagnostics.Single(static x => x.Code == "R003").Message, Does.Contain("missing-opt"));
    }

    [Test]
    public void Resolve_ShouldRemainDeterministic_AcrossRepeatedRuns()
    {
        var resolver = CreateResolver();
        var plan = BuildPlan(
            ["Numbers", "Arithmetic"],
            [new DialectBackendId("interpreter"), new DialectBackendId("compiler"), new DialectBackendId("interpreter")],
            [
                new OptimizerBuildDirective("InterpreterOnlyOpt", true, DialectBackendSelector.For(new DialectBackendId("interpreter"))),
                new OptimizerBuildDirective("CommonOpt", true, DialectBackendSelector.Any)
            ]);

        string? baseline = null;
        for (var i = 0; i < 50; i++)
        {
            var selected = resolver.Resolve(plan);
            var signature = DescribeSelectedPlan(selected);
            baseline ??= signature;
            Assert.That(signature, Is.EqualTo(baseline));
        }
    }

    private static string DescribeSelectedPlan(SelectedRuntimePlan plan)
    {
        return string.Join("|", plan.OrderedModules.Select(x => x.CanonicalAlias))
               + "::"
               + string.Join("|", plan.EnabledBackends.Select(x => x.CanonicalAlias))
               + "::"
               + string.Join("|", plan.EnabledOptimizers.Select(x => x.CanonicalAlias));
    }

    private static DialectBuildPlan BuildPlan(
        IReadOnlyList<string> modules,
        IReadOnlyList<DialectBackendId> backends,
        IReadOnlyList<OptimizerBuildDirective>? optimizers = null) =>
        new(
            "ContractDialect",
            null,
            modules,
            backends,
            [],
            [],
            optimizers ?? [],
            null,
            [],
            new DialectValidationResult([]));

    private static SelectedRuntimePlanResolver CreateResolver()
    {
        var entries = new[]
        {
            Entry(RuntimeComponentKind.FrontendModule, "Arithmetic", "Asm.Modules.Arithmetic"),
            Entry(RuntimeComponentKind.FrontendModule, "Numbers", "Asm.Modules.Numbers"),
            Entry(RuntimeComponentKind.Backend, "compiler", "Asm.Backend.Compiler", "cil"),
            Entry(RuntimeComponentKind.Backend, "interpreter", "Asm.Backend.Interpreter", "vm"),
            Entry(RuntimeComponentKind.Optimizer, "CommonOpt", "Asm.Optimizers.Common"),
            Entry(RuntimeComponentKind.Optimizer, "InterpreterOnlyOpt", "Asm.Optimizers.Interpreter"),
            Entry(RuntimeComponentKind.Optimizer, "CompilerOnlyOpt", "Asm.Optimizers.Compiler")
        };

        return new SelectedRuntimePlanResolver(new StaticCatalog(entries));
    }

    private static RuntimeComponentManifestEntry Entry(RuntimeComponentKind kind, string canonicalAlias, string type, params string[] aliases)
        => new(kind, canonicalAlias, aliases, new RuntimeTypeReference("TestAssembly", type));

    private sealed class StaticCatalog(IEnumerable<RuntimeComponentManifestEntry> entries) : IRuntimeComponentCatalog
    {
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _backends = entries.Where(static x => x.Kind == RuntimeComponentKind.Backend).SelectMany(static x => x.AllAliases.Select(a => (a, x))).ToDictionary(static x => x.a, static x => x.x, StringComparer.Ordinal);
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _modules = entries.Where(static x => x.Kind == RuntimeComponentKind.FrontendModule).SelectMany(static x => x.AllAliases.Select(a => (a, x))).ToDictionary(static x => x.a, static x => x.x, StringComparer.Ordinal);
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _optimizers = entries.Where(static x => x.Kind == RuntimeComponentKind.Optimizer).SelectMany(static x => x.AllAliases.Select(a => (a, x))).ToDictionary(static x => x.a, static x => x.x, StringComparer.Ordinal);

        public bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry) => _modules.TryGetValue(alias, out entry);
        public bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry) => _optimizers.TryGetValue(alias, out entry);
        public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry) => _backends.TryGetValue(alias, out entry);
        public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => _modules.Values.Distinct().OrderBy(static x => x.CanonicalAlias).ToList();
        public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => _optimizers.Values.Distinct().OrderBy(static x => x.CanonicalAlias).ToList();
        public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => _backends.Values.Distinct().OrderBy(static x => x.CanonicalAlias).ToList();
    }
}