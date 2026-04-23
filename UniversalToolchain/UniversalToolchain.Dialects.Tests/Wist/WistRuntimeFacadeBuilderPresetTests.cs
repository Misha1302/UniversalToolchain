using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist.Facade;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.Modules.Tests;

namespace UniversalToolchain.Dialects.Tests.Wist;

[TestFixture]
public sealed class WistRuntimeFacadeBuilderPresetTests
{
    [Test]
    public void WistRuntimeFacadeBuilder_CreateDefault_UsesShippedDefaultPreset()
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .Build();

        var result = wist.Run("price * 0.9 + fee", CreateArguments(), "compiler");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(95.0d).Within(1e-9));
    }

    [Test]
    public void WistRuntimeFacadeBuilder_WithShippedDialectPreset_UsesRequestedPreset()
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .WithShippedDialectPreset(WistShippedDialectPresets.MinimalArithmetic)
            .Build();

        var attempt = wist.TryCompile("2 + 3 * 4", new Dictionary<string, Type>(), "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(attempt.IsSuccess, Is.False);
            Assert.That(attempt.ErrorMessage, Does.Contain("Unknown execution mode 'compiler'"));
        });
    }

    [Test]
    public void WistRuntimeFacadeBuilder_WithDialectFile_OverridesShippedPreset()
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .WithShippedDialectPreset(WistShippedDialectPresets.MinimalArithmetic)
            .WithDialectFile(GetDialectFilePath(WistShippedDialectPresets.FullDefault))
            .Build();

        var result = wist.Run("price * 0.9 + fee", CreateArguments(), "compiler");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(95.0d).Within(1e-9));
    }

    private static Dictionary<string, object?> CreateArguments() =>
        new()
        {
            ["price"] = new RealNumberImpl(100.0d),
            ["fee"] = new RealNumberImpl(5.0d)
        };

    private static string GetDialectFilePath(WistShippedDialectPreset preset)
        => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, preset.RelativeDialectFilePath));
}
