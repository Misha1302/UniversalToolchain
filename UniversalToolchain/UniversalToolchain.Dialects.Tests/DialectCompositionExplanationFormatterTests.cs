using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class DialectCompositionExplanationFormatterTests
{
    [Test]
    public void FormatDeterministic_RepeatedCalls_ReturnSameText()
    {
        var explanation = CreateExplanation();

        var first = DialectCompositionExplanationFormatter.FormatDeterministic(explanation);
        var second = DialectCompositionExplanationFormatter.FormatDeterministic(explanation);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void FormatDeterministic_DoesNotAlphabeticallyReorderOrderedModules()
    {
        var explanation = CreateExplanation(buildModules: ["module-z", "module-a", "module-m"]);

        var text = DialectCompositionExplanationFormatter.FormatDeterministic(explanation);

        Assert.That(text, Does.Contain("Ordered modules: module-z, module-a, module-m"));
    }

    [Test]
    public void FormatDeterministic_KeepsDiagnosticsInOriginalOrder()
    {
        var explanation = CreateExplanation(
            semanticDiagnostics: [Diagnostic("S2"), Diagnostic("S1")],
            resolutionDiagnostics: [Diagnostic("R2"), Diagnostic("R1")]);

        var text = DialectCompositionExplanationFormatter.FormatDeterministic(explanation);

        Assert.Multiple(() =>
        {
            Assert.That(text.IndexOf("S2:", StringComparison.Ordinal), Is.LessThan(text.IndexOf("S1:", StringComparison.Ordinal)));
            Assert.That(text.IndexOf("R2:", StringComparison.Ordinal), Is.LessThan(text.IndexOf("R1:", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void FormatDeterministic_OrdersCapabilitiesDeterministically()
    {
        var explanation = CreateExplanation(capabilities: new Dictionary<string, bool>
        {
            ["z-cap"] = true,
            ["a-cap"] = false
        });

        var text = DialectCompositionExplanationFormatter.FormatDeterministic(explanation);

        Assert.That(text, Does.Contain("Capabilities: a-cap=False, z-cap=True"));
    }

    [Test]
    public void FormatDeterministic_UnknownRuntimeSelection_FormatsHonestFallbackState()
    {
        var runtime = new DialectRuntimeSelectionExplanation(
            "UnknownRuntimeSelection",
            false,
            false,
            [Diagnostic("U1")],
            [],
            [],
            []);
        var explanation = CreateExplanation(runtimeSelection: runtime);

        var text = DialectCompositionExplanationFormatter.FormatDeterministic(explanation);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Runtime components resolved: False"));
            Assert.That(text, Does.Contain("Runtime ordered modules: <not-available>"));
            Assert.That(text, Does.Contain("Runtime enabled backends: <not-available>"));
        });
    }

    private static DialectCompositionExplanation CreateExplanation(
        IReadOnlyList<string>? buildModules = null,
        IReadOnlyList<DialectDiagnostic>? semanticDiagnostics = null,
        IReadOnlyList<DialectDiagnostic>? resolutionDiagnostics = null,
        IReadOnlyDictionary<string, bool>? capabilities = null,
        DialectRuntimeSelectionExplanation? runtimeSelection = null)
    {
        var buildPlan = new DialectBuildPlanExplanation(
            true,
            buildModules ?? ["module-b", "module-a"],
            [new DialectBackendId("compiler"), new DialectBackendId("interpreter")],
            [],
            [new IntrinsicBuildDirective("Math.Abs", true, DialectBackendSelector.Any)],
            [],
            SecurityProfile.Restricted,
            capabilities ?? new Dictionary<string, bool> { ["cap-b"] = true, ["cap-a"] = false });

        runtimeSelection ??= new DialectRuntimeSelectionExplanation(
            "ResolvedSelection",
            true,
            true,
            [],
            [Entry("module-runtime-b"), Entry("module-runtime-a")],
            [Entry("optimizer-a")],
            [Entry("backend-interpreter")]);

        return new DialectCompositionExplanation(
            "source",
            true,
            "Demo",
            "1.0.0",
            buildPlan,
            runtimeSelection,
            semanticDiagnostics ?? [Diagnostic("S1")],
            resolutionDiagnostics ?? [Diagnostic("R1")]);
    }

    private static RuntimeComponentManifestEntry Entry(string alias)
    {
        return new RuntimeComponentManifestEntry(RuntimeComponentKind.FrontendModule, alias, [], new RuntimeComponentId(alias + "-id"), "Assembly");
    }

    private static DialectDiagnostic Diagnostic(string code)
    {
        return new DialectDiagnostic(code, $"{code}-message", DialectDiagnosticSeverity.Warning);
    }
}
