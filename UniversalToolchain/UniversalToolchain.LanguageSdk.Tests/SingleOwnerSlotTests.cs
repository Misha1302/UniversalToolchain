using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// Single-owner slots as a declared law (UT-A007).
/// <para>
/// The project forbids a second parser, a second binder and a second runtime provider. Before this
/// change that law was enforced for <b>backends only</b> (<c>UTL2203</c>); for the other three it relied
/// on every contribution happening to declare <c>Multiplicity = Single</c>. <c>Many</c> is the constructor
/// default, so an external package that said nothing got a second binder and a plan with no diagnostic.
/// </para>
/// <para>
/// The tests are written to distinguish the <b>source</b> of the rejection. A test that only checks
/// "two parsers are rejected" passes against the old code too, because the Wist parser declares
/// <c>Single</c> itself. The decisive fixtures therefore declare <c>Many</c> explicitly: only the slot
/// table can reject those.
/// </para>
/// </summary>
[TestFixture]
public sealed class SingleOwnerSlotTests
{
    private static readonly BackendId Interpreter = new("interpreter");
    private static readonly LanguageArtifactKind<string> Syntax = new("slot.syntax");
    private static readonly LanguageArtifactKind<string> Air = new("slot.air");
    private static readonly LanguageArtifactKind<string> Executable = new("slot.executable");

    [Test]
    public void TwoBindersThatDeclareMany_AreRejectedByTheSlotTable()
    {
        // The reproducer from the finding. Both contributions say Many, which used to mean "the package
        // decided many is fine" and produced a silent plan. Only LanguageSlotTable can reject this.
        var result = Compile(
            LanguageSlots.SemanticsBinding,
            new Edge("probe.bind-a", Syntax, Air, 1),
            new Edge("probe.bind-b", Syntax, Air, 2),
            new Edge("probe.to-exec", Air, Executable, 1));

        Assert.Multiple(() =>
        {
            Assert.That(Codes(result), Does.Contain("UTL2101"),
                "two contributions in a declared single-owner slot must be rejected. " + Format(result));
            var message = Message(result);
            Assert.That(message, Does.Contain("probe.bind-a").And.Contain("probe.bind-b"),
                "the diagnostic must name both owners, or the author cannot tell which to remove");
        });
    }

    [Test]
    public void TheDiagnosticCarriesTheDeclaredRationale_NotAGenericComplaint()
    {
        // "Slot has multiple owners" tells an author what happened. The rationale tells them it is a
        // project law they cannot argue with, as opposed to a choice some package made.
        var result = Compile(
            LanguageSlots.FrontendParser,
            new Edge("probe.parse-a", StandardLanguageArtifactKinds.SourceText, Syntax, 1),
            new Edge("probe.parse-b", StandardLanguageArtifactKinds.SourceText, Syntax, 2),
            new Edge("probe.to-exec", Syntax, Executable, 1));

        Assert.That(Message(result), Does.Contain("one front end"),
            "the message must state the law it enforces. " + Format(result));
    }

    [Test]
    public void DeclaredSingleOwnerSlot_PassesWithExactlyOneOwner_EvenWhenItDeclaresMany()
    {
        // The guard must not become "declared single-owner slots are unusable". A contribution that says
        // Many in such a slot is still fine alone; only a second owner is a violation.
        var result = Compile(
            LanguageSlots.SemanticsBinding,
            new Edge("probe.only-binder", Syntax, Executable, 1));

        Assert.That(result.Diagnostics, Is.Empty, Format(result));
        Assert.That(result.Plan, Is.Not.Null);
    }

    [Test]
    public void LoweringChainWithTwoOwners_IsStillAccepted()
    {
        // Measured, not assumed: Syntax -> Air -> Executable uses two `lowering` contributions and is the
        // normal shape of a pipeline. Putting `lowering` in the single-owner table would have forbidden a
        // legitimate two-step lowering, which is why it is not in the table.
        var result = Compile(
            LanguageSlots.Lowering,
            new Edge("probe.to-air", Syntax, Air, 1),
            new Edge("probe.to-exec", Air, Executable, 1));

        Assert.That(result.Diagnostics, Is.Empty,
            "a two-step lowering chain is legitimate and must stay legal. " + Format(result));
        Assert.That(
            result.Plan!.Routes[Interpreter].Steps.Select(static step => step.ContributionId.Value),
            Is.EqualTo(new[] { "probe.parse", "probe.to-air", "probe.to-exec" }).AsCollection,
            "both lowering steps must appear, in contract order");
    }

    [Test]
    public void ManyOwnerSlots_StillAcceptSeveralContributions()
    {
        // Optimizers are legitimately many. A table that made every slot single-owner would break the
        // composition model this project exists for.
        var package = LanguagePackageBuilder.Create("Slots.Many", "1")
            .AddFeature("slots.many", feature => feature
                .AddTransformer(
                    "slots.many.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    Syntax,
                    static (_, _) => string.Empty,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1,
                    mergePolicy: ContributionMergePolicy.RejectDuplicate,
                    multiplicity: LanguageSlotMultiplicity.Single)
                // Distinct order on purpose: two passes for one contract with EQUAL order is UTL2208, a
                // different rule. The point here is only that two optimizers coexist.
                .AddPass("slots.many.opt-a", LanguageSlots.Optimizers, Syntax,
                    static (value, _) => value, LanguageRuntimeComponentTraits.DeterministicNoHostInterop, order: 10)
                .AddPass("slots.many.opt-b", LanguageSlots.Optimizers, Syntax,
                    static (value, _) => value, LanguageRuntimeComponentTraits.DeterministicNoHostInterop, order: 20)
                .AddBackend(Interpreter.Value, "slots.many.backend", Syntax,
                    static (value, _) => value, LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("slots.many.runtime", "1")
            .Build();

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(Definition("slots.many", "slots.many.runtime").Build());

        Assert.That(result.Diagnostics, Is.Empty, Format(result));
        Assert.That(
            result.Plan!.Contributions.Count(static item => item.Contribution.Slot == LanguageSlots.Optimizers),
            Is.EqualTo(2),
            "both optimizers must remain selected");
    }

    [Test]
    public void UndeclaredSlot_KeepsThePackageLevelContract()
    {
        // A language author may define a slot the project knows nothing about. The table must not invent
        // a restriction there: cardinality stays whatever the package declares.
        var custom = new LanguageSlotId("slots.custom");
        var result = Compile(
            custom,
            new Edge("probe.custom-a", Syntax, Air, 1),
            new Edge("probe.custom-b", Syntax, Air, 2),
            new Edge("probe.to-exec", Air, Executable, 1));

        Assert.That(result.Diagnostics, Is.Empty,
            "an undeclared slot with many-owner contributions must stay legal. " + Format(result));
    }

    [Test]
    public void PackageDeclaredSingle_InAnUndeclaredSlot_KeepsItsOwnDiagnosticText()
    {
        // The package-level rule still exists for slots the table does not cover, and must not be
        // silently replaced by the table's message.
        var custom = new LanguageSlotId("slots.custom.single");
        var package = LanguagePackageBuilder.Create("Slots.CustomSingle", "1")
            .AddFeature("slots.custom.single", feature => feature
                .AddTransformer(
                    "slots.custom.single.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    Syntax,
                    static (_, _) => string.Empty,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1,
                    mergePolicy: ContributionMergePolicy.RejectDuplicate,
                    multiplicity: LanguageSlotMultiplicity.Single)
                .AddTransformer(
                    "slots.custom.a",
                    custom,
                    Syntax,
                    Air,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1,
                    mergePolicy: ContributionMergePolicy.RejectDuplicate,
                    multiplicity: LanguageSlotMultiplicity.Single)
                .AddTransformer(
                    "slots.custom.b",
                    custom,
                    Syntax,
                    Air,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 2)
                .AddBackend(Interpreter.Value, "slots.custom.backend", Air,
                    static (value, _) => value, LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("slots.custom.runtime", "1")
            .Build();

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(Definition("slots.custom.single", "slots.custom.runtime").Build());

        Assert.Multiple(() =>
        {
            Assert.That(Codes(result), Does.Contain("UTL2101"), Format(result));
            Assert.That(Message(result), Does.Contain("multiple owners"),
                "an undeclared slot keeps the package-level wording. " + Format(result));
        });
    }

    [Test]
    public void EveryDeclaredSingleOwnerSlot_IsCoveredByAFixture()
    {
        // A table that grows without a test is a table that stops being true. This fails when a slot is
        // declared single-owner but nobody exercised it.
        var exercised = new[]
        {
            LanguageSlots.FrontendParser,
            LanguageSlots.SemanticsBinding,
            LanguageSlots.RuntimeProvider
        };

        Assert.That(
            LanguageSlotTable.All.Where(static descriptor => descriptor.IsSingleOwner).Select(static descriptor => descriptor.Slot),
            Is.EquivalentTo(exercised),
            "every declared single-owner slot needs a fixture above (runtime.provider is covered by UTL2302, " +
            "which reports ambiguity before slot policies run)");
    }

    [Test]
    public void TableIsOrderedAndNonEmpty()
    {
        var slots = LanguageSlotTable.All.Select(static descriptor => descriptor.Slot.Value).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(slots, Is.Not.Empty, "an empty table would make every slot many-owner again");
            Assert.That(slots, Is.EqualTo(slots.OrderBy(static value => value, StringComparer.Ordinal)).AsCollection,
                "the table must be stable for documentation and diff review");
            Assert.That(LanguageSlotTable.All.All(static descriptor => !string.IsNullOrWhiteSpace(descriptor.Rationale)),
                "a single-owner declaration without a rationale is a rule nobody can review");
        });
    }

    private readonly record struct Edge(string Id, LanguageArtifactKind<string> Source, LanguageArtifactKind<string> Target, int Cost);

    private static LanguageBuildResult Compile(LanguageSlotId slot, params Edge[] edges)
    {
        var package = LanguagePackageBuilder.Create("Slots.Core", "1")
            .AddFeature("slots.core", feature =>
            {
                feature.AddTransformer(
                    "probe.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    Syntax,
                    static (_, _) => string.Empty,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1,
                    mergePolicy: ContributionMergePolicy.RejectDuplicate,
                    multiplicity: LanguageSlotMultiplicity.Single);
                foreach (var edge in edges)
                    feature.AddTransformer(
                        edge.Id,
                        slot,
                        edge.Source,
                        edge.Target,
                        static (value, _) => value,
                        LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                        cost: edge.Cost);
                feature.AddBackend(
                    Interpreter.Value,
                    "probe.backend",
                    Executable,
                    static (value, _) => value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop);
            })
            .UseRouteRuntime("slots.core.runtime", "1")
            .Build();

        return new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(Definition("slots.core", "slots.core.runtime").Build());
    }

    private static LanguageDefinitionBuilder Definition(string feature, string runtime) =>
        LanguageDefinitionBuilder.Create("Slots.Language", "1")
            .UseFeature(feature)
            .EnableBackend(Interpreter)
            .UseRuntimeProvider(runtime, "1");

    private static string[] Codes(LanguageBuildResult result) =>
        result.Diagnostics.Select(static diagnostic => diagnostic.Code).ToArray();

    private static string Message(LanguageBuildResult result) =>
        string.Join(" ", result.Diagnostics.Select(static diagnostic => diagnostic.Message));

    private static string Format(LanguageBuildResult result) =>
        result.Diagnostics.Count == 0
            ? "<no diagnostics>"
            : string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => $"[{diagnostic.Code}] {diagnostic.Message}"));
}
