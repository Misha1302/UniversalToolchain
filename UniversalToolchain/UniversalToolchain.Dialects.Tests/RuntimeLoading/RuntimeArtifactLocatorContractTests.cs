using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class RuntimeArtifactLocatorContractTests
{
    [Test]
    public void ManifestFileLocator_ShouldNotUseAppContextBaseDirectory_WhenFlagIsDisabled()
    {
        using var targetRoot = new TempDirectory();
        var filePath = Path.Combine(targetRoot.Path, "one.dialect.runtime.json");
        File.WriteAllText(filePath, "{}");

        var locator = new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions
        {
            SearchRoots = [targetRoot.Path],
            IncludeAppContextBaseDirectory = false
        });

        var paths = locator.GetManifestFilePaths();

        Assert.That(paths, Is.EqualTo(new[] { Path.GetFullPath(filePath) }));
    }

    [Test]
    public void ManifestFileLocator_ShouldUseAppContextBaseDirectory_WhenFlagIsEnabled()
    {
        using var root = new TempDirectory();
        var inRoot = Path.Combine(root.Path, "in-root.dialect.runtime.json");
        var inBase = Path.Combine(AppContext.BaseDirectory, $"manifest-{Guid.NewGuid():N}.dialect.runtime.json");
        File.WriteAllText(inRoot, "{}");
        File.WriteAllText(inBase, "{}");

        try
        {
            var locator = new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions
            {
                SearchRoots = [root.Path],
                IncludeAppContextBaseDirectory = true
            });

            var paths = locator.GetManifestFilePaths();
            Assert.That(paths, Does.Contain(Path.GetFullPath(inBase)));
        }
        finally
        {
            if (File.Exists(inBase))
                File.Delete(inBase);
        }
    }

    [Test]
    public void ManifestFileLocator_ShouldRespectConfiguredSearchPattern()
    {
        using var root = new TempDirectory();
        var expected = Path.Combine(root.Path, "custom.runtime.json");
        var ignored = Path.Combine(root.Path, "ignored.dialect.runtime.json");
        File.WriteAllText(expected, "{}");
        File.WriteAllText(ignored, "{}");

        var locator = new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions
        {
            SearchRoots = [root.Path],
            IncludeAppContextBaseDirectory = false,
            ManifestSearchPattern = "custom*.runtime.json"
        });

        var paths = locator.GetManifestFilePaths();

        Assert.That(paths, Is.EqualTo(new[] { Path.GetFullPath(expected) }));
    }

    [Test]
    public void AssemblyLocator_ShouldNotSearchOutsideConfiguredRoots()
    {
        using var primary = new TempDirectory();
        using var outside = new TempDirectory();

        File.WriteAllText(Path.Combine(outside.Path, "Sample.dll"), "not-a-real-dll");

        var locator = new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions
        {
            SearchRoots = [primary.Path],
            IncludeAppContextBaseDirectory = false
        });

        var found = locator.TryResolveAssemblyPath("Sample", out _);
        Assert.That(found, Is.False);
    }

    [Test]
    public void AssemblyLocator_ShouldResolveAssemblyPathDeterministically()
    {
        using var first = new TempDirectory();
        using var second = new TempDirectory();
        var firstPath = Path.Combine(first.Path, "Target.dll");
        var secondPath = Path.Combine(second.Path, "Target.dll");
        File.WriteAllText(firstPath, "first");
        File.WriteAllText(secondPath, "second");

        var locator = new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions
        {
            SearchRoots = [second.Path, first.Path],
            IncludeAppContextBaseDirectory = false
        });

        var resolved = locator.TryResolveAssemblyPath("Target", out var absolutePath);

        var expected = new[] { Path.GetFullPath(firstPath), Path.GetFullPath(secondPath) }
            .OrderBy(static x => x, StringComparer.Ordinal)
            .First();

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.True);
            Assert.That(absolutePath, Is.EqualTo(expected));
        });
    }

    [Test]
    public void ArtifactLocatorOptions_ShouldNormalizeSearchRootsDeterministically()
    {
        using var first = new TempDirectory();
        using var second = new TempDirectory();
        var firstManifest = Path.Combine(first.Path, "a.dialect.runtime.json");
        var secondManifest = Path.Combine(second.Path, "b.dialect.runtime.json");
        File.WriteAllText(firstManifest, "{}");
        File.WriteAllText(secondManifest, "{}");

        var locator = new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions
        {
            SearchRoots = [second.Path, first.Path, second.Path],
            IncludeAppContextBaseDirectory = false
        });

        var paths = locator.GetManifestFilePaths();

        Assert.That(paths, Is.EqualTo(new[]
        {
            Path.GetFullPath(firstManifest),
            Path.GetFullPath(secondManifest)
        }));
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dialect-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
