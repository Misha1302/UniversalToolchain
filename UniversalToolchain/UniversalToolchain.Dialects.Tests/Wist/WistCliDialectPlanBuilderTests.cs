using UniversalToolchain.Dialects.Wist.Presets;
using Wistc;

namespace UniversalToolchain.Dialects.Tests.Wist;

public sealed class WistCliDialectPlanBuilderTests
{
    [Test]
    public void Build_WithoutOverrides_ReturnsPresetPlan_ForDefaultPreset()
    {
        var plan = new WistCliDialectPlanBuilder().Build(new CommonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Kind, Is.EqualTo(WistCliDialectPlanKind.Preset));
            Assert.That(plan.BasePreset, Is.SameAs(WistShippedDialectPresets.Default));
            Assert.That(plan.CustomizedDialectText, Is.Null);
        });
    }

    [Test]
    public void Build_WithNativeMath_ReturnsCustomizedPresetPlan_BasedOnFullDefaultNative()
    {
        var plan = new WistCliDialectPlanBuilder().Build(new CommonOptions
        {
            UseNativeMath = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Kind, Is.EqualTo(WistCliDialectPlanKind.CustomizedPreset));
            Assert.That(plan.BasePreset, Is.SameAs(WistShippedDialectPresets.FullDefaultNative));
            Assert.That(plan.CustomizedDialectText, Does.Contain("use "));
        });
    }

    [Test]
    public void Build_WithIncludeModules_ReturnsCustomizedPresetPlan_BasedOnDefault()
    {
        var plan = new WistCliDialectPlanBuilder().Build(new CommonOptions
        {
            IncludeModules = ["ExtraModule"]
        });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Kind, Is.EqualTo(WistCliDialectPlanKind.CustomizedPreset));
            Assert.That(plan.BasePreset, Is.SameAs(WistShippedDialectPresets.Default));
            Assert.That(plan.CustomizedDialectText, Does.Contain("ExtraModule"));
        });
    }

    [Test]
    public void Build_WithExcludeModules_ReturnsCustomizedPresetPlan_BasedOnDefault()
    {
        var plan = new WistCliDialectPlanBuilder().Build(new CommonOptions
        {
            ExcludeModules = ["CSharpInterop"]
        });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Kind, Is.EqualTo(WistCliDialectPlanKind.CustomizedPreset));
            Assert.That(plan.BasePreset, Is.SameAs(WistShippedDialectPresets.Default));
            Assert.That(plan.CustomizedDialectText, Does.Not.Contain("CSharpInterop"));
        });
    }
}
