using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// The `08` A-6 fixture and the backend-coverage invariant that closes it (UT-A004).
/// <para>
/// A contribution scoped to one backend can be selected into a language that does not enable that
/// backend. Today the plan <b>succeeds</b>, the contribution is <b>planned</b>, it appears on <b>no
/// route</b>, and there is <b>no diagnostic</b> — measured below rather than quoted, because the finding
/// existed only as a report and a report is not a reproducer.
/// </para>
/// <para>
/// The fix is an opt-in <see cref="BackendCoverage.Required"/> on the contribution. Opt-in is not
/// timidity: the planner cannot tell an accidental unrouted pass from an intentional cross-backend
/// declaration, and a blanket check would reject currently-working packages and move every existing
/// hash. An author who means "this mechanism must actually run" now says so, and the planner holds them
/// to it.
/// </para>
/// </summary>
[TestFixture]
public sealed class BackendCoverageInvariantTests
{
    private static readonly BackendId Interpreter = new("interpreter");
    private static readonly BackendId Cil = new("cil");
    private static readonly LanguageArtifactKind<int> Syntax = new("coverage.syntax");

    [Test]
    public void AFixture_BackendScopedPassIsPlannedButUnrouted_AndTheDefectIsStillObservable()
    {
        // Records TODAY'S default behaviour, defect included, so the finding keeps a live reproducer.
        // With the invariant opted in the same shape is rejected - that pair is the whole point.
        var (plan, diagnostics) = Build(enabledBackends: [Interpreter], requireCoverage: false);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Empty,
                "the A-6 finding says this plans with NO diagnostic under the default; if that changed, " +
                $"re-scope the finding. Diagnostics: {Format(diagnostics)}");

            var planned = plan!.Contributions.Select(static item => item.Contribution.Id.Value).ToArray();
            Assert.That(planned, Does.Contain("acme.cil.opt"),
                "the cil-only pass is planned even though 'cil' is not enabled");

            var routed = plan.Routes.TryGetValue(Interpreter, out var route)
                ? route.Steps.Select(static step => step.ContributionId.Value).ToArray()
                : [];
            Assert.That(routed, Does.Not.Contain("acme.cil.opt"),
                "the cil-only pass appears on no route - selection promised what the plan cannot deliver");
        });
    }

    [Test]
    public void RequiredCoverage_ForABackendTheLanguageDoesNotEnable_IsRejectedNamingTheContribution()
    {
        var (_, diagnostics) = Build(enabledBackends: [Interpreter], requireCoverage: true);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("UTL2209"),
                $"the invariant must reject this with UTL2209. Diagnostics: {Format(diagnostics)}");
            var message = string.Join(" ", diagnostics.Select(static diagnostic => diagnostic.Message));
            Assert.That(message, Does.Contain("acme.cil.opt"),
                "the diagnostic must name the contribution, or the author cannot find it");
            Assert.That(message, Does.Contain("cil"),
                "the diagnostic must name the backend that is missing coverage");
        });
    }

    [Test]
    public void RequiredCoverage_ForAnEnabledBackend_StaysAcceptedAndIsRouted()
    {
        // The guard must not reject the legitimate case: same package, but 'cil' is enabled.
        var (plan, diagnostics) = Build(enabledBackends: [Interpreter, Cil], requireCoverage: true);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Empty, Format(diagnostics));
            Assert.That(plan, Is.Not.Null);
            Assert.That(
                plan!.Routes[Cil].Steps.Select(static step => step.ContributionId.Value),
                Does.Contain("acme.cil.opt"),
                "with 'cil' enabled and required coverage declared, the pass must actually appear on that route");
        });
    }

    [Test]
    public void UnscopedContribution_IsNotAffectedByTheInvariant()
    {
        // SupportedBackends.Count == 0 means "applies to every backend". An invariant that only looked at
        // the list would either reject these or silently skip them; both are wrong.
        var package = LanguagePackageBuilder.Create("Coverage.Unscoped", "1")
            .AddFeature("coverage.unscoped", feature => feature.AddTransformer(
                "coverage.unscoped.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                Syntax,
                static (source, _) => int.Parse(source),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                cost: 1,
                configure: contribution => contribution.RequireBackendCoverage()))
            .AddBackend(
                Interpreter.Value,
                "coverage.unscoped.backend",
                Syntax,
                static (value, _) => value,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
            .UseRouteRuntime("coverage.unscoped.runtime", "1")
            .Build();

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(LanguageDefinitionBuilder.Create("Coverage.Unscoped.Language", "1")
                .UseFeature("coverage.unscoped")
                .EnableBackend(Interpreter)
                .UseRuntimeProvider("coverage.unscoped.runtime", "1")
                .Build());

        Assert.That(result.Diagnostics, Is.Empty, Format(result.Diagnostics));
    }

    [Test]
    public void PlanningOnlyLanguage_DoesNotTripTheInvariant()
    {
        // No backend enabled means no routes at all, so "coverage" is not a meaningful question yet.
        var package = LanguagePackageBuilder.Create("Coverage.PlanningOnly", "1")
            .AddFeature("coverage.planning", feature => feature.AddTransformer(
                "coverage.planning.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                Syntax,
                static (source, _) => int.Parse(source),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                cost: 1,
                configure: contribution => contribution.RequireBackendCoverage()))
            .AddPass(
                "coverage.planning.opt",
                LanguageSlots.Optimizers,
                Syntax,
                static (value, _) => value,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                supportedBackends: [Cil],
                configure: contribution => contribution.RequireBackendCoverage())
            .Build();

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(LanguageDefinitionBuilder.Create("Coverage.PlanningOnly.Language", "1")
                .UseFeature("coverage.planning")
                .Build());

        Assert.That(result.Diagnostics, Is.Empty,
            "a planning-only language enables no backend, so coverage cannot be missing; " + Format(result.Diagnostics));
    }

    [Test]
    public void RequiredContributionWhoseBackendsAreAllDisabled_IsRejected()
    {
        // Scope and enabled backends are disjoint: the contribution can never run anywhere. Silently
        // treating that as "no coverage needed" would be the same silent partiality one level up.
        var (_, diagnostics) = Build(enabledBackends: [Interpreter], requireCoverage: true, scope: [new BackendId("wasm")]);

        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("UTL2209"),
            $"a required contribution whose backends are all disabled must be rejected. {Format(diagnostics)}");
    }

    [Test]
    public void DefaultCoverage_WritesNoPropertyIntoTheManifest_SoExistingHashesAreUnchanged()
    {
        // ManifestSha256 is computed from the serialized package descriptor, so emitting
        // "backendCoverage": "NotRequired" for every contribution would move every pinned hash.
        var withoutOptIn = Build(enabledBackends: [Interpreter], requireCoverage: false).Plan!;
        var withOptIn = Build(enabledBackends: [Interpreter, Cil], requireCoverage: true).Plan!;

        var defaultValue = withoutOptIn.Contributions
            .Single(static item => item.Contribution.Id.Value == "acme.cil.opt")
            .Contribution.BackendCoverage;
        var optedInValue = withOptIn.Contributions
            .Single(static item => item.Contribution.Id.Value == "acme.cil.opt")
            .Contribution.BackendCoverage;

        Assert.Multiple(() =>
        {
            Assert.That(defaultValue, Is.EqualTo(BackendCoverage.NotRequired));
            Assert.That(optedInValue, Is.EqualTo(BackendCoverage.Required));
            // The manifest text is what hashes; assert the property's absence rather than trusting it.
            var manifest = UniversalToolchain.FeatureSdk.LanguageFeatureManifestSerializer.Serialize(
                new WistPackProbe().Descriptor);
            Assert.That(manifest, Does.Not.Contain("backendCoverage"),
                "no shipped Wist contribution opts in, so no shipped manifest may mention the field");
        });
    }

    private sealed class WistPackProbe
    {
        public UniversalToolchain.FeatureSdk.LanguagePackageDescriptor Descriptor { get; } =
            new UniversalToolchain.Wist.LanguagePack.WistLanguageFeaturePackage().Descriptor;
    }

    private static (LanguagePlan? Plan, IReadOnlyList<LanguageDiagnostic> Diagnostics) Build(
        IReadOnlyList<BackendId> enabledBackends,
        bool requireCoverage,
        IReadOnlyList<BackendId>? scope = null)
    {
        var declaredScope = scope ?? [Cil];
        var package = LanguagePackageBuilder.Create("Coverage.Core", "1")
            .AddFeature("coverage.core", feature => feature
                .AddTransformer(
                    "coverage.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    Syntax,
                    static (source, _) => int.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddPass(
                    "acme.cil.opt",
                    LanguageSlots.Optimizers,
                    Syntax,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    supportedBackends: declaredScope,
                    configure: requireCoverage
                        ? contribution => contribution.RequireBackendCoverage()
                        : null))
            .AddBackend(
                Interpreter.Value,
                "coverage.interpreter",
                Syntax,
                static (value, _) => value,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
            .AddBackend(
                Cil.Value,
                "coverage.cil",
                Syntax,
                static (value, _) => value,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
            .UseRouteRuntime("coverage.runtime", "1")
            .Build();

        var builder = LanguageDefinitionBuilder.Create("Coverage.Language", "1")
            .UseFeature("coverage.core")
            .UseRuntimeProvider("coverage.runtime", "1");
        foreach (var backend in enabledBackends)
            builder = builder.EnableBackend(backend);

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(builder.Build());
        return (result.Plan, result.Diagnostics);
    }

    private static string Format(IEnumerable<LanguageDiagnostic> diagnostics)
    {
        var list = diagnostics.ToArray();
        return list.Length == 0
            ? "<none>"
            : string.Join(Environment.NewLine, list.Select(static diagnostic => $"[{diagnostic.Code}] {diagnostic.Message}"));
    }
}
