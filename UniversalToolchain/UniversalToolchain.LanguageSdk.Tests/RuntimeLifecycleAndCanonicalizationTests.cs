using System.Security.Cryptography;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class RuntimeLifecycleAndCanonicalizationTests
{
    private static readonly LanguageRuntimeComponentTraits SafeTraits =
        LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

    [Test]
    public void TwoRuntimes_DoNotShareMutableTransformerState()
    {
        var fixture = CreateLifecycleFixture(
            _ => new CountingTransformer("lifecycle.parse", StandardLanguageArtifactKinds.SourceText, LifecycleSyntax));

        using var first = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });
        using var second = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });

        var firstValue = first.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)).Value;
        var secondValue = second.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)).Value;

        Assert.Multiple(() =>
        {
            Assert.That(firstValue, Is.EqualTo(1));
            Assert.That(secondValue, Is.EqualTo(1));
        });
    }

    [Test]
    public void RunAfterDispose_ThrowsObjectDisposedException()
    {
        var fixture = CreateLifecycleFixture(
            _ => new CountingTransformer("lifecycle.parse", StandardLanguageArtifactKinds.SourceText, LifecycleSyntax));
        var runtime = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });

        runtime.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            runtime.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)));
    }

    [Test]
    public void Dispose_IsIdempotentAndDisposesOwnedComponentsExactlyOnce()
    {
        var tracker = new DisposalTracker();
        var fixture = CreateLifecycleFixture(
            _ => new DisposableCountingTransformer(
                "lifecycle.parse",
                StandardLanguageArtifactKinds.SourceText,
                LifecycleSyntax,
                tracker));
        var runtime = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });

        runtime.Dispose();
        runtime.Dispose();

        Assert.That(tracker.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public async Task DisposeAsync_IsIdempotentAndDisposesAsyncComponentsExactlyOnce()
    {
        var tracker = new DisposalTracker();
        var fixture = CreateLifecycleFixture(
            _ => new AsyncDisposableCountingTransformer(
                "lifecycle.parse",
                StandardLanguageArtifactKinds.SourceText,
                LifecycleSyntax,
                tracker));
        var runtime = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });

        await runtime.DisposeAsync();
        await runtime.DisposeAsync();

        Assert.That(tracker.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void PerSessionFactory_ReusingInstanceIsRejected()
    {
        var shared = new CountingTransformer(
            "lifecycle.parse",
            StandardLanguageArtifactKinds.SourceText,
            LifecycleSyntax);
        var fixture = CreateLifecycleFixture(_ => shared);

        using var first = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package }));

        Assert.That(exception!.Message, Does.Contain("reused an instance"));
    }

    [Test]
    public void SingletonStateless_RequiresExplicitMarker()
    {
        var package = LanguagePackageBuilder.Create("Singleton.Language", "1")
            .AddFeature("singleton.core", feature => feature
                .AddTransformerFactory(
                    "singleton.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    LifecycleSyntax,
                    _ => new CountingTransformer(
                        "singleton.parse",
                        StandardLanguageArtifactKinds.SourceText,
                        LifecycleSyntax),
                    SafeTraits,
                    lifetime: LanguageRuntimeComponentLifetime.SingletonStateless)
                .AddBackend(
                    LifecycleBackend,
                    new LanguageContributionId("singleton.backend"),
                    LifecycleSyntax,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("singleton.runtime", "1")
            .Build();
        var plan = CreatePlanForPackage(package);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package }));

        Assert.That(exception!.Message, Does.Contain(nameof(IStatelessLanguageRuntimeComponent)));
    }

    [Test]
    public void TypedAndUntypedContracts_DoNotConnect()
    {
        var kind = new LanguageArtifactKindId("contracts.value");
        var typed = new LanguageArtifactContract(kind, LanguageTypeIdentity.For<string>());
        var untyped = new LanguageArtifactContract(kind);

        Assert.Multiple(() =>
        {
            Assert.That(LanguageArtifactRoute.ContractsConnect(typed, untyped), Is.False);
            Assert.That(LanguageArtifactRoute.ContractsConnect(untyped, typed), Is.False);
            Assert.That(LanguageArtifactRoute.ContractsConnect(untyped, untyped), Is.True);
            Assert.That(LanguageArtifactRoute.ContractsConnect(typed, typed), Is.True);
        });
    }

    [Test]
    public void TypedRoute_RejectsUntypedBridgeDuringConstruction()
    {
        var source = StandardLanguageArtifactKinds.SourceText.Contract;
        var middleId = new LanguageArtifactKindId("contracts.middle");
        var legacyMiddle = new LanguageArtifactContract(middleId);
        var typedMiddle = new LanguageArtifactContract(middleId, LanguageTypeIdentity.For<int>());
        var target = new LanguageArtifactKind<int>("contracts.target").Contract;
        var backend = new BackendId("contracts");

        Assert.Throws<ArgumentException>(() => new LanguageArtifactRoute(
            backend,
            source,
            target,
            [
                new LanguageArtifactRouteStep(new LanguageContributionId("contracts.first"), source, legacyMiddle, 1),
                new LanguageArtifactRouteStep(new LanguageContributionId("contracts.second"), typedMiddle, target, 1)
            ]));
    }

    [Test]
    public void CanonicalManifestHash_IsIndependentOfPrettyNewLines()
    {
        var package = CreateCanonicalPackage().Descriptor;
        var lf = LanguageFeatureManifestSerializer.Serialize(package);
        var crlf = lf.Replace("\n", "\r\n", StringComparison.Ordinal);
        var canonical = LanguageFeatureManifestSerializer.SerializeCanonical(package);
        var expected = Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();

        Assert.Multiple(() =>
        {
            Assert.That(lf, Does.Not.Contain("\r"));
            Assert.That(canonical, Does.Not.Contain((byte)'\r').And.Not.Contain((byte)'\n'));
            Assert.That(LanguageFeatureManifestSerializer.ComputeSha256(package), Is.EqualTo(expected));
            Assert.That(
                LanguageFeatureManifestSerializer.ComputeSha256(LanguageFeatureManifestSerializer.Deserialize(lf)),
                Is.EqualTo(LanguageFeatureManifestSerializer.ComputeSha256(LanguageFeatureManifestSerializer.Deserialize(crlf))));
        });
    }

    [Test]
    public void ManifestSchemaV5_DeclaresCanonicalizationAndHashAlgorithm()
    {
        var json = LanguageFeatureManifestSerializer.Serialize(CreateCanonicalPackage().Descriptor);

        Assert.Multiple(() =>
        {
            Assert.That(LanguageFeatureManifestSerializer.SchemaVersion, Is.EqualTo(5));
            Assert.That(json, Does.Contain("\"canonicalization\": \"universaltoolchain-json-v1\""));
            Assert.That(json, Does.Contain("\"hashAlgorithm\": \"sha256\""));
        });
    }

    [Test]
    public void LockFileSerialization_UsesStableLfAndCanonicalBytes()
    {
        var package = CreateCanonicalPackage();
        var plan = CreatePlanForPackage(package);
        var pretty = LanguageLockFile.Serialize(plan);
        var canonical = LanguageLockFile.SerializeCanonical(plan);

        Assert.Multiple(() =>
        {
            Assert.That(LanguageLockFile.SchemaVersion, Is.EqualTo(5));
            Assert.That(pretty, Does.Not.Contain("\r"));
            Assert.That(pretty, Does.EndWith("\n"));
            Assert.That(canonical, Does.Not.Contain((byte)'\r').And.Not.Contain((byte)'\n'));
        });
    }

    [Test]
    public void CanonicalValidationManifests_ContainAllSdkTestsAndPackages()
    {
        var root = FindRepositoryRoot();
        var tests = ReadManifest(Path.Combine(root, "eng", "test-projects.txt"));
        var packages = ReadManifest(Path.Combine(root, "eng", "package-projects.txt"));

        Assert.Multiple(() =>
        {
            Assert.That(tests, Has.Count.EqualTo(4));
            Assert.That(tests, Does.Contain("UniversalToolchain/UniversalToolchain.LanguageSdk.Tests/UniversalToolchain.LanguageSdk.Tests.csproj"));
            Assert.That(packages, Has.Count.EqualTo(9));
            Assert.That(packages, Does.Contain("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/UniversalToolchain.Wist.LanguagePack.csproj"));
            Assert.That(packages, Does.Contain("UniversalToolchain/UniversalToolchain.Templates/UniversalToolchain.Templates.csproj"));
        });
    }

    private static readonly BackendId LifecycleBackend = new("lifecycle");
    private static readonly LanguageArtifactKind<int> LifecycleSyntax = new("lifecycle.syntax");

    private static LifecycleFixture CreateLifecycleFixture(
        Func<LanguageRuntimeComponentContext, ILanguageArtifactTransformer<string, int>> transformerFactory)
    {
        var package = LanguagePackageBuilder.Create("Lifecycle.Language", "1")
            .AddFeature("lifecycle.core", feature => feature
                .AddTransformerFactory(
                    "lifecycle.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    LifecycleSyntax,
                    transformerFactory,
                    SafeTraits)
                .AddBackend(
                    LifecycleBackend,
                    new LanguageContributionId("lifecycle.backend"),
                    LifecycleSyntax,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("lifecycle.runtime", "1")
            .Build();
        return new LifecycleFixture(package, CreatePlanForPackage(package));
    }

    private static AuthoredLanguagePackage CreateCanonicalPackage() =>
        LanguagePackageBuilder.Create("Canonical.Language", "1")
            .WithMetadata("z", "last")
            .WithMetadata("a", "first")
            .AddFeature("canonical.core", feature => feature
                .AddTransformer(
                    "canonical.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    LifecycleSyntax,
                    static (source, _) => int.Parse(source),
                    SafeTraits)
                .AddBackend(
                    LifecycleBackend,
                    new LanguageContributionId("canonical.backend"),
                    LifecycleSyntax,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("canonical.runtime", "1")
            .Build();

    private static LanguagePlan CreatePlanForPackage(AuthoredLanguagePackage package) =>
        new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create(package.PackageId.Value, package.PackageVersion.Value)
                .UseFeature(package.Descriptor.Features.Single().Id)
                .EnableBackend(LifecycleBackend)
                .UseRuntimeProvider(package.RuntimeProvider!.ProviderId, package.RuntimeProvider.Version)
                .Build()).GetRequiredPlan();

    private static IReadOnlyList<string> ReadManifest(string path) =>
        File.ReadAllLines(path)
            .Select(static line => line.Trim())
            .Where(static line => line.Length != 0 && !line.StartsWith('#'))
            .ToArray();

    private static string FindRepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("UT_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(Path.Combine(configured, "readme.md")))
            return Path.GetFullPath(configured);

        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory, TestContext.CurrentContext.WorkDirectory })
        {
            var current = new DirectoryInfo(start);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "readme.md")))
                current = current.Parent;
            if (current != null)
                return current.FullName;
        }
        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record LifecycleFixture(AuthoredLanguagePackage Package, LanguagePlan Plan);

    private sealed class CountingTransformer(
        string contributionId,
        LanguageArtifactKind<string> source,
        LanguageArtifactKind<int> target) : ILanguageArtifactTransformer<string, int>
    {
        private int _count;
        public LanguageContributionId ContributionId { get; } = new(contributionId);
        public LanguageArtifactKind<string> TypedSourceKind { get; } = source;
        public LanguageArtifactKind<int> TypedTargetKind { get; } = target;
        public LanguageRuntimeComponentTraits TypedTraits => SafeTraits;
        public int Transform(string value, LanguageArtifactTransformationContext context) => ++_count;
    }

    private sealed class DisposableCountingTransformer(
        string contributionId,
        LanguageArtifactKind<string> source,
        LanguageArtifactKind<int> target,
        DisposalTracker tracker) : ILanguageArtifactTransformer<string, int>, IDisposable
    {
        public LanguageContributionId ContributionId { get; } = new(contributionId);
        public LanguageArtifactKind<string> TypedSourceKind { get; } = source;
        public LanguageArtifactKind<int> TypedTargetKind { get; } = target;
        public LanguageRuntimeComponentTraits TypedTraits => SafeTraits;
        public int Transform(string value, LanguageArtifactTransformationContext context) => 1;
        public void Dispose() => tracker.RecordDispose();
    }

    private sealed class AsyncDisposableCountingTransformer(
        string contributionId,
        LanguageArtifactKind<string> source,
        LanguageArtifactKind<int> target,
        DisposalTracker tracker) : ILanguageArtifactTransformer<string, int>, IAsyncDisposable
    {
        public LanguageContributionId ContributionId { get; } = new(contributionId);
        public LanguageArtifactKind<string> TypedSourceKind { get; } = source;
        public LanguageArtifactKind<int> TypedTargetKind { get; } = target;
        public LanguageRuntimeComponentTraits TypedTraits => SafeTraits;
        public int Transform(string value, LanguageArtifactTransformationContext context) => 1;
        public ValueTask DisposeAsync()
        {
            tracker.RecordDispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class DisposalTracker
    {
        private int _disposeCount;
        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public void RecordDispose() => Interlocked.Increment(ref _disposeCount);
    }

}
