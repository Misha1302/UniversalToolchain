using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
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
    public async Task ReentrantDispose_FailsImmediatelyWithoutDeadlock()
    {
        LanguageRuntime? runtime = null;
        var fixture = CreateLifecycleFixture(
            _ => new CallbackTransformer(
                "lifecycle.parse",
                StandardLanguageArtifactKinds.SourceText,
                LifecycleSyntax,
                () => runtime!.Dispose()));
        runtime = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });

        var runTask = Task.Run(() => runtime.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)));
        var completed = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(3)));

        Assert.That(completed, Is.SameAs(runTask), "Reentrant disposal must not self-deadlock.");
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await runTask);
        Assert.That(exception!.Message, Does.Contain("currently owns one of its operation leases"));

        runtime.Dispose();
    }

    [Test]
    public async Task FlowedChildContext_AfterOperationCompletion_DoesNotRetainActiveLease()
    {
        LanguageRuntime? runtime = null;
        Task? childTask = null;
        var childReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseChild = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fixture = CreateLifecycleFixture(
            _ => new CallbackTransformer(
                "lifecycle.parse",
                StandardLanguageArtifactKinds.SourceText,
                LifecycleSyntax,
                () =>
                {
                    childTask = Task.Run(async () =>
                    {
                        childReady.TrySetResult(true);
                        await releaseChild.Task.ConfigureAwait(false);
                        runtime!.Dispose();
                    });
                }));
        runtime = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });

        var result = runtime.Run(new LanguageExecutionRequest("ignored", LifecycleBackend));
        await childReady.Task.WaitAsync(TimeSpan.FromSeconds(3));
        releaseChild.TrySetResult(true);
        await childTask!.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.That(result.Value, Is.EqualTo(1));
        Assert.Throws<ObjectDisposedException>(() =>
            runtime.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)));
    }

    [Test]
    public void SessionConstruction_WhenCleanupAlsoFails_PreservesPrimaryExceptionFirst()
    {
        var cleanupSource = StandardLanguageArtifactKinds.SourceText;
        var cleanupSyntax = new LanguageArtifactKind<int>("cleanup.syntax");
        var cleanupBackend = new BackendId("cleanup");
        var package = LanguagePackageBuilder.Create("Cleanup.Language", "1")
            .AddFeature("cleanup.core", feature => feature
                .AddTransformerFactory(
                    "cleanup.a-owned",
                    LanguageSlots.FrontendParser,
                    cleanupSource,
                    cleanupSyntax,
                    _ => new ThrowingDisposeTransformer(
                        "cleanup.a-owned",
                        cleanupSource,
                        cleanupSyntax),
                    SafeTraits)
                .AddPassFactory(
                    "cleanup.b-fail",
                    LanguageSlots.Optimizers,
                    cleanupSyntax,
                    static _ => throw new InvalidOperationException("primary construction failure"),
                    SafeTraits)
                .AddBackend(
                    cleanupBackend,
                    new LanguageContributionId("cleanup.backend"),
                    cleanupSyntax,
                    static (value, _) => value,
                    SafeTraits))
            .UseRouteRuntime("cleanup.runtime", "1")
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(
            LanguageDefinitionBuilder.Create("Cleanup.Language", "1")
                .UseFeature("cleanup.core")
                .EnableBackend(cleanupBackend)
                .UseRuntimeProvider("cleanup.runtime", "1")
                .Build()).GetRequiredPlan();

        var exception = Assert.Throws<AggregateException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package }));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerExceptions, Has.Count.EqualTo(2));
            Assert.That(exception.InnerExceptions[0].Message, Is.EqualTo("primary construction failure"));
            Assert.That(exception.InnerExceptions[1].Message, Is.EqualTo("cleanup disposal failure"));
        });
    }

    [Test]
    public async Task ExternalConcurrentDispose_WaitsForInFlightOperationAndRejectsNewOperations()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var fixture = CreateLifecycleFixture(
            _ => new BlockingTransformer(
                "lifecycle.parse",
                StandardLanguageArtifactKinds.SourceText,
                LifecycleSyntax,
                entered,
                release));
        var runtime = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });
        var runTask = Task.Run(() => runtime.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)));
        Assert.That(entered.Wait(TimeSpan.FromSeconds(3)), Is.True);

        var disposeTask = Task.Run(runtime.Dispose);
        Assert.That(disposeTask.Wait(TimeSpan.FromMilliseconds(100)), Is.False);
        Assert.Throws<ObjectDisposedException>(() =>
            runtime.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)));

        release.Set();
        await Task.WhenAll(runTask, disposeTask).WaitAsync(TimeSpan.FromSeconds(3));
        Assert.That(runTask.Result.Value, Is.EqualTo(1));
    }

    [Test]
    public async Task ConcurrentDispose_BoundedStressRemainsDeterministic()
    {
        const int iterations = 32;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var fixture = CreateLifecycleFixture(
                _ => new BlockingTransformer(
                    "lifecycle.parse",
                    StandardLanguageArtifactKinds.SourceText,
                    LifecycleSyntax,
                    entered,
                    release));
            var runtime = LanguageRuntime.Create(fixture.Plan, new ILanguageRouteComponentSource[] { fixture.Package });
            var runTask = Task.Run(() => runtime.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)));
            Assert.That(entered.Wait(TimeSpan.FromSeconds(1)), Is.True, $"Iteration {iteration} did not enter the operation.");

            var disposeTask = Task.Run(runtime.Dispose);
            await Task.Delay(5);
            Assert.That(disposeTask.IsCompleted, Is.False, $"Iteration {iteration} disposed before the lease was released.");
            Assert.Throws<ObjectDisposedException>(() =>
                runtime.Run(new LanguageExecutionRequest("ignored", LifecycleBackend)));

            release.Set();
            await Task.WhenAll(runTask, disposeTask).WaitAsync(TimeSpan.FromSeconds(1));
            Assert.That(runTask.Result.Value, Is.EqualTo(1));
        }
    }

    [Test]
    public void LanguagePlan_HasNoPublicConstructor_AndVerifierDetectsHashTampering()
    {
        var plan = CreatePlanForPackage(CreateCanonicalPackage());
        var backingField = typeof(LanguagePlan).GetField("<PlanHash>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(typeof(LanguagePlan).GetConstructors(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
        Assert.That(backingField, Is.Not.Null);
        backingField!.SetValue(plan, new string('0', 64));

        Assert.Throws<LanguagePlanVerificationException>(() => LanguagePlanVerifier.Verify(plan));
    }

    [Test]
    public void PlanHash_IsIndependentOfFeatureAndBackendInsertionOrder()
    {
        var package = LanguagePackageBuilder.Create("Canonical.Order", "1")
            .AddFeature("canonical.a", feature => feature.AddTransformer(
                "canonical.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                LifecycleSyntax,
                static (source, _) => int.Parse(source),
                SafeTraits))
            .AddFeature("canonical.b", feature => feature.AddPass(
                "canonical.pass",
                LanguageSlots.Optimizers,
                LifecycleSyntax,
                static (value, _) => value,
                SafeTraits))
            .AddBackend(LifecycleBackend.Value, "canonical.backend", LifecycleSyntax, static (value, _) => value, SafeTraits)
            .UseRouteRuntime("canonical.runtime", "1")
            .Build();
        var registry = new LanguagePackageRegistry().AddPackage(package);
        var compiler = new LanguageCompiler(registry);
        var first = compiler.Compile(LanguageDefinitionBuilder.Create("Canonical.Order", "1")
            .UseFeature("canonical.a").UseFeature("canonical.b")
            .EnableBackend(LifecycleBackend)
            .UseRuntimeProvider("canonical.runtime", "1")
            .Build()).GetRequiredPlan();
        var second = compiler.Compile(LanguageDefinitionBuilder.Create("Canonical.Order", "1")
            .UseFeature("canonical.b").UseFeature("canonical.a")
            .EnableBackend(LifecycleBackend)
            .UseRuntimeProvider("canonical.runtime", "1")
            .Build()).GetRequiredPlan();

        Assert.That(second.PlanHash, Is.EqualTo(first.PlanHash));
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
        var tests = ReadTestProjects(Path.Combine(root, "eng", "test-counts.json"));
        var packages = ReadManifest(Path.Combine(root, "eng", "package-projects.txt"));

        Assert.Multiple(() =>
        {
            Assert.That(tests, Has.Count.EqualTo(6));
            Assert.That(tests, Does.Contain("UniversalToolchain/UniversalToolchain.LanguageSdk.Tests/UniversalToolchain.LanguageSdk.Tests.csproj"));
            Assert.That(tests, Does.Contain("UniversalToolchain/UniversalToolchain.PlanFuzz.Tests/UniversalToolchain.PlanFuzz.Tests.csproj"));
            Assert.That(tests, Does.Contain("UniversalToolchain/UniversalToolchain.PlanFuzz.IntegrationTests/UniversalToolchain.PlanFuzz.IntegrationTests.csproj"));
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

    private static IReadOnlyList<string> ReadTestProjects(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement
            .GetProperty("main")
            .EnumerateArray()
            .Concat(document.RootElement.GetProperty("isolated").EnumerateArray())
            .Select(static entry => entry.GetProperty("path").GetString()!)
            .Select(static path => path.EndsWith(".dll", StringComparison.Ordinal)
                ? ToProjectPath(path)
                : path)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ToProjectPath(string assemblyPath)
    {
        var projectDirectory = assemblyPath[..assemblyPath.IndexOf("/bin/", StringComparison.Ordinal)];
        var projectName = projectDirectory[(projectDirectory.LastIndexOf('/') + 1)..];
        return $"{projectDirectory}/{projectName}.csproj";
    }

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

    private sealed class CallbackTransformer(
        string contributionId,
        LanguageArtifactKind<string> source,
        LanguageArtifactKind<int> target,
        Action callback) : ILanguageArtifactTransformer<string, int>
    {
        public LanguageContributionId ContributionId { get; } = new(contributionId);
        public LanguageArtifactKind<string> TypedSourceKind { get; } = source;
        public LanguageArtifactKind<int> TypedTargetKind { get; } = target;
        public LanguageRuntimeComponentTraits TypedTraits => SafeTraits;

        public int Transform(string value, LanguageArtifactTransformationContext context)
        {
            callback();
            return 1;
        }
    }

    private sealed class BlockingTransformer(
        string contributionId,
        LanguageArtifactKind<string> source,
        LanguageArtifactKind<int> target,
        ManualResetEventSlim entered,
        ManualResetEventSlim release) : ILanguageArtifactTransformer<string, int>
    {
        public LanguageContributionId ContributionId { get; } = new(contributionId);
        public LanguageArtifactKind<string> TypedSourceKind { get; } = source;
        public LanguageArtifactKind<int> TypedTargetKind { get; } = target;
        public LanguageRuntimeComponentTraits TypedTraits => SafeTraits;

        public int Transform(string value, LanguageArtifactTransformationContext context)
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(3)))
                throw new TimeoutException("Test operation release was not signalled.");
            return 1;
        }
    }

    private sealed class ThrowingDisposeTransformer(
        string contributionId,
        LanguageArtifactKind<string> source,
        LanguageArtifactKind<int> target) : ILanguageArtifactTransformer<string, int>, IDisposable
    {
        public LanguageContributionId ContributionId { get; } = new(contributionId);
        public LanguageArtifactKind<string> TypedSourceKind { get; } = source;
        public LanguageArtifactKind<int> TypedTargetKind { get; } = target;
        public LanguageRuntimeComponentTraits TypedTraits => SafeTraits;
        public int Transform(string value, LanguageArtifactTransformationContext context) => 1;
        public void Dispose() => throw new InvalidOperationException("cleanup disposal failure");
    }

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
