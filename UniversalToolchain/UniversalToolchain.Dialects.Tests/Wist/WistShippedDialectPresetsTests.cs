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
}
