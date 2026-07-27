using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Dialects.Tests.Wist;

[TestFixture]
public sealed class WistShippedDialectPresetsTests
{
    [Test]
    public void WistShippedDialectPresets_AllIds_AreUnique()
    {
        var ids = WistShippedDialectPresets.All.Select(x => x.Id).ToArray();

        Assert.That(ids, Is.Unique);
    }

    [Test]
    public void WistShippedDialectPresets_Default_IsPresentInAll()
    {
        Assert.That(
            WistShippedDialectPresets.All.Any(x => x.Id == WistShippedDialectPresets.Default.Id),
            Is.True);
    }

    [Test]
    public void WistShippedDialectPresets_GetRequired_KnownId_ReturnsPreset()
    {
        var preset = WistShippedDialectPresets.GetRequired(WistShippedDialectPresets.MinimalArithmetic.Id);

        Assert.That(preset, Is.SameAs(WistShippedDialectPresets.MinimalArithmetic));
    }

    [Test]
    public void WistShippedDialectPresets_GetRequired_UnknownId_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => WistShippedDialectPresets.GetRequired("missing-preset"));

        Assert.That(ex!.Message, Does.Contain("Unknown shipped Wist dialect preset 'missing-preset'."));
    }

    [Test]
    public void WistShippedDialectPresets_CatalogExactlyMatchesPackagedDialectDirectories()
    {
        var catalogIds = WistShippedDialectPresets.All
            .Select(static preset => preset.Id)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var fileIds = Directory.EnumerateFiles(
                TestSourcePaths.WistExamplesRoot,
                "dialect.wistdialect",
                SearchOption.AllDirectories)
            .Select(static path => new DirectoryInfo(Path.GetDirectoryName(path)!).Name)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.That(catalogIds, Is.EqualTo(fileIds));
    }

    [Test]
    public void WistShippedDialectPresets_BackendContracts_AreCanonicalAndSelfConsistent()
    {
        foreach (var preset in WistShippedDialectPresets.All)
        {
            Assert.Multiple(() =>
            {
                Assert.That(preset.SupportedBackends, Is.Not.Empty, preset.Id);
                Assert.That(preset.SupportedBackends, Is.Unique, preset.Id);
                Assert.That(preset.SupportedBackends, Is.EqualTo(preset.SupportedBackends.OrderBy(static backend => backend, StringComparer.Ordinal)), preset.Id);
                Assert.That(preset.SupportedBackends, Has.All.Matches<string>(static backend =>
                    backend is "cil" or "interpreter"), preset.Id);
                Assert.That(preset.SupportedBackends, Does.Contain(preset.DefaultBackend), preset.Id);
            });
        }
    }
}