using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// Negative tests over provider admission (UT-A017).
/// <para>
/// A plan is built against an exact runtime provider identity, and execution must refuse anything else.
/// Those refusals existed with no tests, which is the dangerous state: each check is cheap to weaken
/// during a refactor, and a weakened check does not fail loudly — it <em>succeeds</em>, and the wrong
/// component runs against a plan it was never selected for.
/// </para>
/// <para>
/// The assertions below record where each refusal actually happens, which turned out to be two
/// different layers. A mismatched <em>identity</em> never reaches the runtime: the provider registry is
/// keyed by the provider's own (id, version), so a foreign identity simply is not found. A mismatched
/// <em>contract</em> (API major, contribution id, missing policy validator) is refused by the runtime
/// after lookup. Writing these tests confirmed that split rather than assuming it — the first draft
/// expected the runtime's identity check to fire and it does not.
/// </para>
/// <para>
/// No production code changes here. If a test exposes a real gap it becomes its own PR, not a quiet
/// edit.
/// </para>
/// </summary>
[TestFixture]
public sealed class RuntimeProviderAdversarialTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void ForeignProviderId_IsRejectedByTheRegistryBeforeAnySessionIsCreated()
    {
        var (plan, provider) = BuildPlanAndProvider();
        var impostor = new DelegatingProvider(provider)
        {
            ProviderId = new LanguageRuntimeProviderId("wist.runtime.impostor")
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => LanguageRuntime.Create(plan, new LanguageRuntimeProviderRegistry().AddProvider(impostor)));

        Assert.Multiple(() =>
        {
            // The registry reports the identity the plan requires, because that is the key it looked up.
            Assert.That(exception!.Message, Does.Contain(provider.ProviderId.Value),
                "the refusal must name the provider identity the plan requires");
            Assert.That(exception.Message, Does.Contain("not registered"),
                "a foreign identity is a lookup miss, not a contract violation; conflating the two " +
                "would send a caller looking in the wrong place");
        });
    }

    [Test]
    public void WrongProviderVersion_IsRejectedAndTheMessageListsTheRegisteredVersions()
    {
        var (plan, provider) = BuildPlanAndProvider();
        var impostor = new DelegatingProvider(provider)
        {
            ProviderVersion = new LanguageVersion("999.0.0-impostor")
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => LanguageRuntime.Create(plan, new LanguageRuntimeProviderRegistry().AddProvider(impostor)));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain(provider.ProviderVersion.Value),
                "the required version must appear in the message");
            Assert.That(exception.Message, Does.Contain("999.0.0-impostor"),
                "the registered versions must be listed; without them the caller cannot see that a " +
                "different version is what is actually installed");
        });
    }

    [Test]
    public void WrongToolchainApiMajor_IsRejectedByTheRuntime()
    {
        var (plan, provider) = BuildPlanAndProvider();
        var impostor = new DelegatingProvider(provider)
        {
            ToolchainApiVersion = new ToolchainApiVersion(plan.Definition.ToolchainApiVersion.Major + 1)
        };

        // Identity is preserved, so the registry finds it and the runtime's contract checks run. This is
        // the layer that a weakened identity check would have to be replaced by.
        var exception = Assert.Throws<InvalidOperationException>(
            () => LanguageRuntime.Create(plan, new LanguageRuntimeProviderRegistry().AddProvider(impostor)));

        Assert.That(exception!.Message, Does.Contain("API version"),
            "a Toolchain API mismatch must be named as such: silent acceptance means a plan built for " +
            "one contract executes under another");
    }

    [Test]
    public void ProviderImplementingADifferentContribution_IsRejectedAndTheMessageNamesBoth()
    {
        var (plan, provider) = BuildPlanAndProvider();
        var planned = plan.RuntimeProviderContribution!.Contribution.Id;
        var impostor = new DelegatingProvider(provider)
        {
            RuntimeContributionId = new LanguageContributionId("wist.runtime.impostor.contribution")
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => LanguageRuntime.Create(plan, new LanguageRuntimeProviderRegistry().AddProvider(impostor)));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("wist.runtime.impostor.contribution"),
                "the message must name what the provider actually implements");
            Assert.That(exception.Message, Does.Contain(planned.Value),
                "the message must name what the plan selected");
        });
    }

    [Test]
    public void IdentityMismatchAndContractMismatch_AreReportedAsDifferentProblems()
    {
        // A tempting simplification is to collapse every admission failure into one message. They are
        // different conditions with different remediation, so they must stay distinguishable.
        var (plan, provider) = BuildPlanAndProvider();
        var foreignIdentity = new DelegatingProvider(provider)
        {
            ProviderId = new LanguageRuntimeProviderId("wist.runtime.impostor")
        };
        var foreignContract = new DelegatingProvider(provider)
        {
            RuntimeContributionId = new LanguageContributionId("wist.runtime.impostor.contribution")
        };

        var identityFailure = Assert.Throws<InvalidOperationException>(
            () => LanguageRuntime.Create(plan, new LanguageRuntimeProviderRegistry().AddProvider(foreignIdentity)));
        var contractFailure = Assert.Throws<InvalidOperationException>(
            () => LanguageRuntime.Create(plan, new LanguageRuntimeProviderRegistry().AddProvider(foreignContract)));

        Assert.Multiple(() =>
        {
            Assert.That(identityFailure!.Message, Is.Not.EqualTo(contractFailure!.Message),
                "two unrelated admission failures produced the same message, which hides the cause");
            Assert.That(identityFailure.Message, Does.Contain("not registered"));
            Assert.That(contractFailure.Message, Does.Contain("implements contribution"));
        });
    }

    private static (LanguagePlan Plan, ILanguageRuntimeProvider Provider) BuildPlanAndProvider()
    {
        var package = new WistLanguageFeaturePackage();
        var definition = WistFacadeLanguageDefinitionFactory.FromPreset(
            WistLanguageDefinitions.FullDefaultId,
            Interpreter.Value,
            WistFacadeSsaPolicy.Disabled);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();

        var provider = LanguageRouteRuntimeAssembler.CreateProvider(
            plan,
            new ILanguageRouteComponentSource[] { package });
        return (plan, provider);
    }

    /// <summary>
    /// Forwards every member to a real provider while allowing one field to be corrupted, so each test
    /// changes exactly one thing.
    /// </summary>
    private sealed class DelegatingProvider(ILanguageRuntimeProvider inner) : ILanguageRuntimeProvider
    {
        public LanguageRuntimeProviderId ProviderId { get; init; } = inner.ProviderId;
        public LanguageVersion ProviderVersion { get; init; } = inner.ProviderVersion;
        public ToolchainApiVersion ToolchainApiVersion { get; init; } = inner.ToolchainApiVersion;
        public LanguageContributionId RuntimeContributionId { get; init; } = inner.RuntimeContributionId;
        public IReadOnlyCollection<BackendId> SupportedBackends => inner.SupportedBackends;

        public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options) =>
            inner.CreateSession(plan, options);
    }
}
