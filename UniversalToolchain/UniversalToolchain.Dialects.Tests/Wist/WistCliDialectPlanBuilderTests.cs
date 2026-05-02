using UniversalToolchain.Dialects.Wist.Presets;
using Wistc;

namespace UniversalToolchain.Dialects.Tests.Wist;

public sealed class WistCliDialectPlanBuilderTests
{
    [Test]
    public void Build_ReturnsPresetPlan_WithoutDialectTextMutation()
    {
        var plan = new WistCliDialectPlanBuilder().Build(new CommonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Kind, Is.EqualTo(WistCliDialectPlanKind.Preset));
            Assert.That(plan.BasePreset, Is.SameAs(WistShippedDialectPresets.Default));
            Assert.That(plan.CustomizedDialectText, Is.Null);
        });
    }
}