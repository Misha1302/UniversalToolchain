using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class RuntimeConstructionFailureTests
{
    [Test]
    public void Rethrow_PrimaryOnly_PreservesOriginalExceptionAndStack()
    {
        var primary = CapturePrimary();

        var observed = Assert.Throws<InvalidOperationException>(() =>
            RuntimeConstructionFailure.Rethrow(primary, [], "aggregate"));

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.SameAs(primary));
            Assert.That(observed!.StackTrace, Does.Contain(nameof(CapturePrimary)));
        });
    }

    [Test]
    public void Rethrow_PrimaryAndCleanup_PutsPrimaryFirst()
    {
        var primary = CapturePrimary();
        var cleanup = new ApplicationException("cleanup");

        var aggregate = Assert.Throws<AggregateException>(() =>
            RuntimeConstructionFailure.Rethrow(primary, [cleanup], "aggregate"));

        Assert.Multiple(() =>
        {
            Assert.That(aggregate!.InnerExceptions, Has.Count.EqualTo(2));
            Assert.That(aggregate.InnerExceptions[0], Is.SameAs(primary));
            Assert.That(aggregate.InnerExceptions[1], Is.SameAs(cleanup));
            Assert.That(primary.StackTrace, Does.Contain(nameof(CapturePrimary)));
        });
    }

    [Test]
    public void DisposeSynchronouslyCollect_CollectsMultipleFailuresInReverseOwnershipOrder()
    {
        var calls = new List<string>();
        var first = new ThrowingDisposable("first", calls);
        var second = new ThrowingDisposable("second", calls);

        var failures = RuntimeConstructionFailure.DisposeSynchronouslyCollect(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(new[] { "second", "first" }));
            Assert.That(failures.Select(static exception => exception.Message),
                Is.EqualTo(new[] { "second cleanup", "first cleanup" }));
        });
    }

    [Test]
    public async Task DisposeAsynchronouslyCollect_PrefersAsyncDisposeAndCollectsFailures()
    {
        var owner = new ThrowingAsyncDisposable();

        var failures = await RuntimeConstructionFailure.DisposeAsynchronouslyCollect(owner);

        Assert.Multiple(() =>
        {
            Assert.That(owner.AsyncDisposeCount, Is.EqualTo(1));
            Assert.That(owner.SyncDisposeCount, Is.Zero);
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0].Message, Is.EqualTo("async cleanup"));
        });
    }

    [Test]
    public void LanguageRuntime_PublicSurface_DoesNotExposeArtifactBuildOperations()
    {
        var publicMethods = typeof(LanguageRuntime)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(static method => method.Name)
            .ToHashSet(StringComparer.Ordinal);
        var componentSourceFactory = typeof(LanguageRuntime)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(LanguageRuntime.Create) &&
                method.GetParameters().Length >= 2 &&
                method.GetParameters()[1].ParameterType == typeof(IEnumerable<ILanguageRouteComponentSource>));

        Assert.Multiple(() =>
        {
            Assert.That(publicMethods, Does.Not.Contain("Build"));
            Assert.That(publicMethods, Does.Not.Contain("ExecuteBuilt"));
            Assert.That(publicMethods, Does.Not.Contain("GetBuiltArtifactValue"));
            Assert.That(componentSourceFactory.ReturnType, Is.EqualTo(typeof(LanguageBuildRuntime)));
        });
    }

    private static InvalidOperationException CapturePrimary()
    {
        try
        {
            ThrowPrimary();
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }

        throw new AssertionException("unreachable");
    }

    private static void ThrowPrimary() => throw new InvalidOperationException("primary");

    private sealed class ThrowingDisposable(string name, ICollection<string> calls) : IDisposable
    {
        public void Dispose()
        {
            calls.Add(name);
            throw new ApplicationException($"{name} cleanup");
        }
    }

    private sealed class ThrowingAsyncDisposable : IDisposable, IAsyncDisposable
    {
        public int SyncDisposeCount { get; private set; }
        public int AsyncDisposeCount { get; private set; }

        public void Dispose()
        {
            SyncDisposeCount++;
            throw new ApplicationException("sync cleanup");
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposeCount++;
            return ValueTask.FromException(new ApplicationException("async cleanup"));
        }
    }
}
