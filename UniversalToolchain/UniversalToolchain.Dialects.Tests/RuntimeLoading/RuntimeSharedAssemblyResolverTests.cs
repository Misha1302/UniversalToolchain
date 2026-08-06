using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

[TestFixture]
[NonParallelizable]
public sealed class RuntimeSharedAssemblyResolverTests
{
    private static readonly Assembly HostAssembly = typeof(IRuntimeSharedAssemblyResolver).Assembly;

    [Test]
    public void Resolve_CompatibleSharedAssembly_ReturnsHostAssembly()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        var resolver = Resolver(HostAssembly);

        var result = resolver.Resolve(HostAssembly.GetName(), fixture.Path);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(RuntimeSharedAssemblyResolutionKind.Shared));
            Assert.That(result.Assembly, Is.SameAs(HostAssembly));
        });
    }

    [Test]
    public void Resolve_UnregisteredAssembly_ReturnsNotShared()
    {
        var resolver = new DefaultRuntimeSharedAssemblyResolver([]);

        var result = resolver.Resolve(HostAssembly.GetName(), HostAssembly.Location);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(RuntimeSharedAssemblyResolutionKind.NotShared));
            Assert.That(result.Assembly, Is.Null);
        });
    }

    [Test]
    public void Resolve_VersionMismatch_FailsClosed()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        var requested = CloneIdentity(HostAssembly.GetName());
        requested.Version = new Version((requested.Version?.Major ?? 1) + 1, 0, 0, 0);

        var ex = Assert.Throws<InvalidOperationException>(() => Resolver(HostAssembly).Resolve(requested, fixture.Path));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("Expected host identity"));
            Assert.That(ex.Message, Does.Contain("requested identity"));
            Assert.That(ex.Message, Does.Contain("Isolated fallback is forbidden"));
        });
    }

    [Test]
    public void Resolve_PublicKeyTokenMismatch_FailsClosed()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        var requested = CloneIdentity(HostAssembly.GetName());
        requested.SetPublicKeyToken([0x01, 0x23, 0x45, 0x67]);

        var ex = Assert.Throws<InvalidOperationException>(() => Resolver(HostAssembly).Resolve(requested, fixture.Path));

        Assert.That(ex!.Message, Does.Contain("requested identity"));
    }

    [Test]
    public void Resolve_CultureMismatch_FailsClosed()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        var requested = CloneIdentity(HostAssembly.GetName());
        requested.CultureName = "fr-FR";

        var ex = Assert.Throws<InvalidOperationException>(() => Resolver(HostAssembly).Resolve(requested, fixture.Path));

        Assert.That(ex!.Message, Does.Contain("requested identity"));
    }

    [Test]
    public void Resolve_SameIdentityDifferentBytes_FailsStrictIntegrity()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly, mutateBytes: true);
        var resolver = Resolver(HostAssembly);

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(HostAssembly.GetName(), fixture.Path));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("strict content integrity"));
            Assert.That(ex.Message, Does.Contain("Expected SHA-256"));
            Assert.That(ex.Message, Does.Contain("actual SHA-256"));
        });
    }

    [Test]
    public void Descriptor_CustomLoadContextAssembly_IsRejected()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        AssertCustomContextAssemblyIsRejected(fixture.Path);
    }

    [Test]
    public void Descriptor_DynamicAssembly_IsRejected()
    {
        var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("RuntimeSharedAssemblyResolverTests.Dynamic"),
            AssemblyBuilderAccess.Run);

        var ex = Assert.Throws<InvalidOperationException>(() => RuntimeSharedAssemblyDescriptor.Create(dynamicAssembly));

        Assert.That(ex!.Message, Does.Contain("Dynamic assemblies"));
    }

    [Test]
    public void Resolver_DuplicateCompatibleRegistration_IsIdempotent()
    {
        var descriptor = RuntimeSharedAssemblyDescriptor.Create(HostAssembly);

        Assert.DoesNotThrow(() => _ = new DefaultRuntimeSharedAssemblyResolver([descriptor, descriptor]));
    }

    [Test]
    public void Resolver_ConflictingSnapshot_FailsAtConstruction()
    {
        var descriptor = RuntimeSharedAssemblyDescriptor.Create(HostAssembly);
        var conflicting = descriptor with { Sha256 = new string('0', 64) };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _ = new DefaultRuntimeSharedAssemblyResolver([descriptor, conflicting]));

        Assert.That(ex!.Message, Does.Contain("not a valid immutable snapshot"));
    }

    [Test]
    public void Loader_UnregisteredRoot_RemainsInCollectibleIsolatedContext()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        AssertUnregisteredRootRemainsCollectible(fixture.Path);
    }

    [Test]
    public void Loader_RegisteredRoot_UsesHostIdentity()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        using var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new SingleAssemblyLocator(HostAssembly.GetName().Name!, fixture.Path),
            Resolver(HostAssembly));

        var loaded = strategy.LoadAssembly(HostAssembly.GetName().Name!);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.SameAs(HostAssembly));
            Assert.That(AssemblyLoadContext.GetLoadContext(loaded), Is.SameAs(AssemblyLoadContext.Default));
        });
    }

    [Test]
    public void Loader_ParallelRootLoad_PublishesSingleAssemblyInstance()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        AssertParallelRootLoadPublishesSingleInstance(fixture.Path);
    }

    [Test]
    public void Loader_AfterDispose_RejectsNewLoads()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new SingleAssemblyLocator(HostAssembly.GetName().Name!, fixture.Path),
            new DefaultRuntimeSharedAssemblyResolver([]));
        strategy.Dispose();

        Assert.Throws<ObjectDisposedException>(() => strategy.LoadAssembly(HostAssembly.GetName().Name!));
    }

    [Test]
    public void Loader_Dispose_AllowsCollectibleContextToUnload()
    {
        using var fixture = ConfiguredCopy.Create(HostAssembly);
        var context = LoadAndDispose(fixture.Path);

        for (var attempt = 0; attempt < 80 && context.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(10);
        }

        Assert.That(context.IsAlive, Is.False);
    }

    private static DefaultRuntimeSharedAssemblyResolver Resolver(params Assembly[] assemblies) =>
        new(assemblies.Select(RuntimeSharedAssemblyDescriptor.Create));

    private static AssemblyName CloneIdentity(AssemblyName source) => new(source.FullName!);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertCustomContextAssemblyIsRejected(string path)
    {
        var context = new AssemblyLoadContext("RuntimeSharedAssemblyResolverTests.Custom", isCollectible: true);
        try
        {
            var customAssembly = context.LoadFromAssemblyPath(path);
            var ex = Assert.Throws<InvalidOperationException>(() =>
                RuntimeSharedAssemblyDescriptor.Create(customAssembly));
            Assert.That(ex!.Message, Does.Contain("AssemblyLoadContext.Default"));
        }
        finally
        {
            context.Unload();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertUnregisteredRootRemainsCollectible(string path)
    {
        using var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new SingleAssemblyLocator(HostAssembly.GetName().Name!, path),
            new DefaultRuntimeSharedAssemblyResolver([]));

        var loaded = strategy.LoadAssembly(HostAssembly.GetName().Name!);
        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Not.SameAs(HostAssembly));
            Assert.That(AssemblyLoadContext.GetLoadContext(loaded)?.Name, Is.EqualTo("UniversalToolchain.Runtime.Isolated"));
            Assert.That(AssemblyLoadContext.GetLoadContext(loaded)?.IsCollectible, Is.True);
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertParallelRootLoadPublishesSingleInstance(string path)
    {
        using var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new SingleAssemblyLocator(HostAssembly.GetName().Name!, path),
            new DefaultRuntimeSharedAssemblyResolver([]));

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() => strategy.LoadAssembly(HostAssembly.GetName().Name!)))
            .ToArray();
        var assemblies = Task.WhenAll(tasks).GetAwaiter().GetResult();

        Assert.That(assemblies.All(assembly => ReferenceEquals(assembly, assemblies[0])), Is.True);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadAndDispose(string path)
    {
        var simpleName = HostAssembly.GetName().Name!;
        var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new SingleAssemblyLocator(simpleName, path),
            new DefaultRuntimeSharedAssemblyResolver([]));
        var loaded = strategy.LoadAssembly(simpleName);
        var context = AssemblyLoadContext.GetLoadContext(loaded)!;
        var weak = new WeakReference(context, trackResurrection: false);
        strategy.Dispose();
        return weak;
    }

    private static void DeleteDirectoryAfterCollectibleUnload(string directory)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
                return;
            }
            catch (UnauthorizedAccessException) when (attempt < 79)
            {
            }
            catch (IOException) when (attempt < 79)
            {
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(10);
        }

        Directory.Delete(directory, recursive: true);
    }

    private sealed class SingleAssemblyLocator(string simpleName, string path) : IRuntimeAssemblyLocator
    {
        public bool TryResolveAssemblyPath(string assemblySimpleName, out string? absolutePath)
        {
            absolutePath = string.Equals(assemblySimpleName, simpleName, StringComparison.Ordinal) ? path : null;
            return absolutePath is not null;
        }
    }

    private sealed class ConfiguredCopy : IDisposable
    {
        private readonly string _directory;

        private ConfiguredCopy(string directory, string path)
        {
            _directory = directory;
            Path = path;
        }

        public string Path { get; }

        public static ConfiguredCopy Create(Assembly assembly, bool mutateBytes = false)
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wist-runtime-shared-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var path = System.IO.Path.Combine(directory, System.IO.Path.GetFileName(assembly.Location));
            File.Copy(assembly.Location, path);
            if (mutateBytes)
            {
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None);
                stream.WriteByte(0xA5);
            }

            return new ConfiguredCopy(directory, path);
        }

        public void Dispose() => DeleteDirectoryAfterCollectibleUnload(_directory);
    }
}
