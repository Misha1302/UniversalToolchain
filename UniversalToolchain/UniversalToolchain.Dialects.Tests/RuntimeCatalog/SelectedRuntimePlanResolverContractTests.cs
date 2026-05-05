using UniversalToolchain.Dialects.Abstractions;

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

    [Test]
    public void Resolve_RepeatedRuns_PreservesDeterministicDiagnosticsOrdering()
    {
        var resolver = CreateResolver();
        var plan = BuildPlan(
            ["MissingModule", "Arithmetic"],
            [new DialectBackendId("missing-backend"), new DialectBackendId("interpreter")],
            [new OptimizerBuildDirective("missing-opt", true, DialectBackendSelector.Any)],
            [new DialectDiagnostic("V001", "Existing validation diagnostic.", DialectDiagnosticSeverity.Warning)]);

        string? baseline = null;
        for (var i = 0; i < 50; i++)
        {
            var selected = resolver.Resolve(plan);
            var signature = DescribeSelectedPlan(selected);
            baseline ??= signature;
            Assert.That(signature, Is.EqualTo(baseline));
        }

        var diagnostics = resolver.Resolve(plan).Diagnostics
            .Select(static x => $"{x.Code}:{x.Message}")
            .ToArray();

        Assert.That(diagnostics, Is.EqualTo(new[]
        {
            "V001:Existing validation diagnostic.",
            "R001:Runtime module descriptor 'MissingModule' was not registered.",
            "R002:Runtime backend descriptor 'missing-backend' was not registered.",
            "R003:Runtime optimizer descriptor 'missing-opt' was not registered."
        }));
    }

    private static string DescribeSelectedPlan(SelectedRuntimePlan plan)
    {
        return string.Join("|", plan.OrderedModules.Select(x => x.CanonicalAlias))
               + "::"
               + string.Join("|", plan.EnabledBackends.Select(x => x.CanonicalAlias))
               + "::"
               + string.Join("|", plan.EnabledOptimizers.Select(x => x.CanonicalAlias))
               + "::"
               + string.Join("|", plan.Diagnostics.Select(static x => $"{x.Code}:{x.Severity}:{x.Message}"));
    }

    private static DialectBuildPlan BuildPlan(
        IReadOnlyList<string> modules,
        IReadOnlyList<DialectBackendId> backends,
        IReadOnlyList<OptimizerBuildDirective>? optimizers = null,
        IReadOnlyList<DialectDiagnostic>? diagnostics = null) =>
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
            new DialectValidationResult(diagnostics ?? []));

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

    private static RuntimeComponentManifestEntry Entry(RuntimeComponentKind kind, string canonicalAlias, string _, params string[] aliases)
        => new(kind, canonicalAlias, aliases, RuntimeComponentIdFactory.Create(kind, canonicalAlias), "TestAssembly");

    private sealed class StaticCatalog : IRuntimeComponentCatalog
    {
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _backends;
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _modules;
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _optimizers;

        public StaticCatalog(IEnumerable<RuntimeComponentManifestEntry> entries)
        {
            var entryList = entries.ToArray();
            _backends = BuildMap(entryList, RuntimeComponentKind.Backend);
            _modules = BuildMap(entryList, RuntimeComponentKind.FrontendModule);
            _optimizers = BuildMap(entryList, RuntimeComponentKind.Optimizer);
        }

        public bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry) => _modules.TryGetValue(alias, out entry);
        public bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry) => _optimizers.TryGetValue(alias, out entry);
        public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry) => _backends.TryGetValue(alias, out entry);
        public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => _modules.Values.Distinct().OrderBy(static x => x.CanonicalAlias).ToList();
        public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => _optimizers.Values.Distinct().OrderBy(static x => x.CanonicalAlias).ToList();
        public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => _backends.Values.Distinct().OrderBy(static x => x.CanonicalAlias).ToList();

        private static IReadOnlyDictionary<string, RuntimeComponentManifestEntry> BuildMap(
            IEnumerable<RuntimeComponentManifestEntry> entries,
            RuntimeComponentKind kind)
            => entries
                .Where(x => x.Kind == kind)
                .SelectMany(static x => x.AllAliases.Select(a => (a, x)))
                .ToDictionary(static x => x.a, static x => x.x, StringComparer.Ordinal);
    }
}