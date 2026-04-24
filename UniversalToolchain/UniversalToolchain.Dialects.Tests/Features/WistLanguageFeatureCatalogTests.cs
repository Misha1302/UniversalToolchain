using UniversalToolchain.Dialects.Wist.Features;

namespace UniversalToolchain.Dialects.Tests.Features;

[TestFixture]
public sealed class WistLanguageFeatureCatalogTests
{
    [Test]
    public void GetFeatures_ReturnsDeterministicOrder()
    {
        var catalog = new WistLanguageFeatureCatalog();

        var ids = catalog.GetFeatures()
            .Select(static x => x.FeatureId.Value)
            .ToArray();

        Assert.That(ids, Is.EqualTo(ids.Order(StringComparer.Ordinal).ToArray()));
    }

    [Test]
    public void TryGetFeature_KnownFeature_ReturnsDescriptor()
    {
        var catalog = new WistLanguageFeatureCatalog();

        var result = catalog.TryGetFeature(
            WistLanguageFeatureIds.CSharpInterop,
            out var descriptor);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor!.FeatureId, Is.EqualTo(WistLanguageFeatureIds.CSharpInterop));
        });
    }

    [Test]
    public void GetFeatures_DoesNotContainDuplicateIds()
    {
        var catalog = new WistLanguageFeatureCatalog();

        var ids = catalog.GetFeatures()
            .Select(static x => x.FeatureId.Value)
            .ToArray();

        Assert.That(ids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(ids.Length));
    }
}
