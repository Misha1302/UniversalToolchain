using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class DialectCompositionExplanationProjectorTests
{
    [Test]
    public void Project_WhenResultContainsBuildPlan_ProjectsGenericBuildSnapshot()
    {
        var result = new DialectFrameworkCompositionResult(
            "source",
            null,
            CreateBuildPlan(["B", "A"]),
            [],
            []);

        var explanation = DialectCompositionExplanationProjector.Project(result);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.BuildPlan, Is.Not.Null);
            Assert.That(explanation.BuildPlan!.CanBuild, Is.True);
            Assert.That(explanation.BuildPlan.EnabledBackends.Select(static x => x.Value), Is.EqualTo(new[] { "compiler" }));
        });
    }

    [Test]
    public void Project_WhenResultContainsUnknownRuntimeSelection_KeepsRuntimeSelectionWithoutInventingResolvedEntries()
    {
        var runtimeSelection = new UnknownRuntimeSelection(true, [Diagnostic("U1")]);
        var result = new DialectFrameworkCompositionResult("source", null, null, [], [], runtimeSelection);

        var explanation = DialectCompositionExplanationProjector.Project(result);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.RuntimeSelection, Is.Not.Null);
            Assert.That(explanation.RuntimeSelection!.HasResolvedRuntimeComponents, Is.False);
            Assert.That(explanation.RuntimeSelection.OrderedModules, Is.Empty);
            Assert.That(explanation.RuntimeSelection.Diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { "U1" }));
        });
    }

    [Test]
    public void Project_WhenResultContainsResolvedRuntimeSelection_PopulatesResolvedRuntimeEntries()
    {
        var runtimeSelection = new SelectedRuntimePlan(
            [Entry("module-b"), Entry("module-a")],
            [Entry("opt-a")],
            [Entry("backend-interpreter")],
            []);
        var result = new DialectFrameworkCompositionResult("source", null, null, [], [], runtimeSelection);

        var explanation = DialectCompositionExplanationProjector.Project(result);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.RuntimeSelection, Is.Not.Null);
            Assert.That(explanation.RuntimeSelection!.HasResolvedRuntimeComponents, Is.True);
            Assert.That(explanation.RuntimeSelection.OrderedModules.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "module-b", "module-a" }));
            Assert.That(explanation.RuntimeSelection.EnabledBackends.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "backend-interpreter" }));
        });
    }

    [Test]
    public void Project_PreservesBuildPlanOrderedModulesOrder()
    {
        var result = new DialectFrameworkCompositionResult(
            "source",
            null,
            CreateBuildPlan(["Third", "First", "Second"]),
            [],
            []);

        var explanation = DialectCompositionExplanationProjector.Project(result);
        Assert.That(explanation.BuildPlan!.OrderedModules, Is.EqualTo(new[] { "Third", "First", "Second" }));
    }

    [Test]
    public void Project_PreservesRuntimeSelectionOrderedModulesOrder()
    {
        var runtimeSelection = new SelectedRuntimePlan(
            [Entry("three"), Entry("one"), Entry("two")],
            [],
            [],
            []);
        var result = new DialectFrameworkCompositionResult("source", null, null, [], [], runtimeSelection);

        var explanation = DialectCompositionExplanationProjector.Project(result);
        Assert.That(explanation.RuntimeSelection!.OrderedModules.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "three", "one", "two" }));
    }

    [Test]
    public void Project_PreservesDiagnosticsOrder()
    {
        var semantic = new[] { Diagnostic("S2"), Diagnostic("S1") };
        var resolution = new[] { Diagnostic("R2"), Diagnostic("R1") };
        var runtimeSelection = new UnknownRuntimeSelection(true, [Diagnostic("U2"), Diagnostic("U1")]);
        var result = new DialectFrameworkCompositionResult("source", null, null, semantic, resolution, runtimeSelection);

        var explanation = DialectCompositionExplanationProjector.Project(result);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.SemanticDiagnostics.Select(static x => x.Code), Is.EqualTo(new[] { "S2", "S1" }));
            Assert.That(explanation.ResolutionDiagnostics.Select(static x => x.Code), Is.EqualTo(new[] { "R2", "R1" }));
            Assert.That(explanation.RuntimeSelection!.Diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { "U2", "U1" }));
        });
    }

    private static DialectBuildPlan CreateBuildPlan(IReadOnlyList<string> orderedModules)
    {
        return new DialectBuildPlan(
            "Demo",
            "1.0.0",
            orderedModules,
            [new DialectBackendId("compiler")],
            [new DialectBackendId("legacy")],
            [new IntrinsicBuildDirective("Math.Abs", true, DialectBackendSelector.Any)],
            [new OptimizerBuildDirective("LocalVariablesOptimization", true, DialectBackendSelector.Any)],
            SecurityProfile.Restricted,
            [new KeyValuePair<string, bool>("unsafe-interop", false)],
            new DialectValidationResult());
    }

    private static RuntimeComponentManifestEntry Entry(string alias)
    {
        return new RuntimeComponentManifestEntry(RuntimeComponentKind.FrontendModule, alias, [], new RuntimeComponentId(alias + "-id"), "Assembly");
    }

    private static DialectDiagnostic Diagnostic(string code)
    {
        return new DialectDiagnostic(code, $"{code}-message", DialectDiagnosticSeverity.Warning);
    }

    private sealed class UnknownRuntimeSelection(bool isResolved, IReadOnlyList<DialectDiagnostic> diagnostics) : IDialectRuntimeSelection
    {
        public bool IsResolved { get; } = isResolved;

        public IReadOnlyList<DialectDiagnostic> Diagnostics { get; } = diagnostics;
    }
}
