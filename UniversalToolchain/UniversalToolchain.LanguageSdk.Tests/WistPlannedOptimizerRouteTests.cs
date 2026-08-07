using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistPlannedOptimizerRouteTests
{
    private static readonly BackendId Interpreter = new("interpreter");
    private static readonly BackendId Cil = new("cil");

    [Test]
    public void ShippedPresets_CompileExactlyOneTypedSsaPolicy()
    {
        var compiler = CreateCompiler();

        foreach (var presetId in WistLanguageDefinitions.PresetIds)
        {
            var plan = compiler.Compile(WistLanguageDefinitions.Create(presetId)).GetRequiredPlan();
            var selectedPolicy = plan.Features
                .Select(static feature => feature.Feature.Id)
                .Where(WistSsaPolicyFeatureIds.All.Contains)
                .ToArray();

            Assert.That(selectedPolicy, Has.Length.EqualTo(1), presetId);
            Assert.That(
                selectedPolicy[0],
                Is.EqualTo(presetId == WistLanguageDefinitions.SsaId
                    ? WistSsaPolicyFeatureIds.Require
                    : WistSsaPolicyFeatureIds.Disabled),
                presetId);
        }
    }

    [Test]
    public void SelectedOptimizers_AreExplicitAirPassesInPlannerOwnedOrderOnBothBackends()
    {
        var plan = CreateCompiler().Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.SsaId)).GetRequiredPlan();
        var plannedOptimizers = plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.Optimizers)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(plannedOptimizers, Is.Not.Empty);
            Assert.That(plannedOptimizers.Select(static contribution => contribution.Contribution.Order), Is.Ordered.Ascending);
            Assert.That(plannedOptimizers.All(static contribution =>
                contribution.Contribution.Transformation is { IsPass: true } transformation &&
                transformation.SourceContract == WistArtifactKinds.AirContract &&
                transformation.TargetContract == WistArtifactKinds.AirContract), Is.True);
        });

        foreach (var backend in new[] { Interpreter, Cil })
        {
            var route = plan.Routes[backend];
            var expected = new[]
            {
                WistContributionIds.Frontend,
                WistContributionIds.LoweringToBytecode,
                WistContributionIds.LoweringToAir
            }
                .Concat(plannedOptimizers.Select(static contribution => contribution.Contribution.Id))
                .Append(backend == Interpreter
                    ? WistContributionIds.InterpreterBackend
                    : WistContributionIds.CilBackend)
                .ToArray();

            Assert.That(route.Steps.Select(static step => step.ContributionId), Is.EqualTo(expected), backend.Value);
        }
    }

    [Test]
    public void SsaPreset_CompilesRequirePolicyIntoPlanAndRuntimeOptions()
    {
        var plan = CreateCompiler().Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.SsaId)).GetRequiredPlan();
        var options = WistSsaPlanPolicy.CreateRuntimeOptions(plan);

        Assert.Multiple(() =>
        {
            Assert.That(options.Policy.ToString(), Is.EqualTo("Require"));
            Assert.That(options.Diagnostics.ToString(), Is.EqualTo("Default"));
            Assert.That(plan.Routes.Values.All(route => route.Steps.Any(static step =>
                step.ContributionId == WistContributionIds.SsaOptimizer)), Is.True);
        });
    }

    [Test]
    public void ConflictingTypedSsaPolicies_FailDuringPlanning()
    {
        var definition = CreateSsaDefinition(
            "wist.ssa.policy.conflict",
            WistSsaPolicyFeatureIds.Require,
            WistSsaPolicyFeatureIds.Prefer);

        var result = CreateCompiler().Compile(definition);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("UTL1002"));
        });
    }

    [Test]
    public void SsaPolicyFeature_ChangesPlanIdentityWithoutChangingOptimizerPassSet()
    {
        var compiler = CreateCompiler();
        var require = compiler.Compile(CreateSsaDefinition(
            "wist.ssa.policy.identity",
            WistSsaPolicyFeatureIds.Require)).GetRequiredPlan();
        var prefer = compiler.Compile(CreateSsaDefinition(
            "wist.ssa.policy.identity",
            WistSsaPolicyFeatureIds.Prefer)).GetRequiredPlan();

        var requireOptimizers = require.Contributions
            .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.Optimizers)
            .Select(static contribution => contribution.Contribution.Id)
            .ToArray();
        var preferOptimizers = prefer.Contributions
            .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.Optimizers)
            .Select(static contribution => contribution.Contribution.Id)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(requireOptimizers, Is.EqualTo(preferOptimizers));
            Assert.That(require.PlanHash, Is.Not.EqualTo(prefer.PlanHash));
            Assert.That(WistSsaPlanPolicy.CreateRuntimeOptions(require).Policy.ToString(), Is.EqualTo("Require"));
            Assert.That(WistSsaPlanPolicy.CreateRuntimeOptions(prefer).Policy.ToString(), Is.EqualTo("Prefer"));
        });
    }

    [Test]
    public void OpaqueSsaMetadata_CannotOverrideTypedPlanPolicy()
    {
        var definition = LanguageDefinitionBuilder
            .Create("wist.ssa.opaque-metadata", WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .UseFeature(WistFeatureIds.Arithmetic)
            .UseFeature(WistSsaPolicyFeatureIds.Disabled)
            .WithMetadata("wist.ssa.policy", "Require")
            .EnableBackend(Interpreter)
            .Build();
        var plan = CreateCompiler().Compile(definition).GetRequiredPlan();

        Assert.Multiple(() =>
        {
            Assert.That(WistSsaPlanPolicy.CreateRuntimeOptions(plan).Policy.ToString(), Is.EqualTo("Off"));
            Assert.That(plan.Contributions.Any(static contribution =>
                contribution.Contribution.Id == WistContributionIds.SsaOptimizer), Is.False);
        });
    }

    private static LanguageCompiler CreateCompiler() =>
        new(new LanguagePackageRegistry().AddPackage(new WistLanguageFeaturePackage()));

    private static LanguageDefinition CreateSsaDefinition(
        string id,
        params LanguageFeatureId[] policies)
    {
        var builder = LanguageDefinitionBuilder
            .Create(id, WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .UseFeature(WistFeatureIds.Identifiers)
            .UseFeature(WistFeatureIds.NativeTypes)
            .UseFeature(WistFeatureIds.Scopes)
            .UseFeature(WistFeatureIds.Variables)
            .UseFeature(WistFeatureIds.Whitespaces)
            .UseFeature(WistFeatureIds.ArithmeticOptimization)
            .UseFeature(WistFeatureIds.EGraphOptimization)
            .UseFeature(WistFeatureIds.NativeCilOptimization)
            .UseFeature(WistFeatureIds.NativeTypesOptimization)
            .UseFeature(WistFeatureIds.SsaOptimization)
            .EnableBackend(Interpreter)
            .EnableBackend(Cil);
        foreach (var policy in policies)
            builder.UseFeature(policy);
        return builder.Build();
    }
}
