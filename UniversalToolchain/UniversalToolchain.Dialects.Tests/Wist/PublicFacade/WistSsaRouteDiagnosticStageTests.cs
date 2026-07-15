using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistSsaRouteDiagnosticStageTests
{
    [Test]
    public void RouteDiagnostic_TwoArgumentConstructorPreservesLegacyShape()
    {
        var diagnostic = new WistSsaRouteDiagnostic("test.code", "message");
        var (code, message) = diagnostic;

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo("test.code"));
            Assert.That(message, Is.EqualTo("message"));
            Assert.That(diagnostic.Stage, Is.Null);
        });
    }

    [Test]
    public void OptimizationReport_PreservesExplicitRouteStage()
    {
        var report = new WistSsaOptimizationReport(
            WistSsaPolicy.Prefer,
            usedSsa: false,
            fellBackToAir: true,
            profile: "test-profile",
            inputAirInstructionCount: 3,
            outputAirInstructionCount: 3,
            diagnostics:
            [
                new WistSsaRouteDiagnostic(
                    "ssa.to-air.value-reuse.unsupported",
                    "Repeated value requires scheduling.",
                    "emission")
            ]);

        Assert.That(report.Diagnostics.Single().Stage, Is.EqualTo("emission"));
    }

    [Test]
    public void TryCompile_WhenSsaLoweringIsUnsupported_ReportsFacadeAndRouteStages()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            Optimization = new WistOptimizationOptions
            {
                Ssa = new WistSsaOptions
                {
                    Policy = WistSsaPolicy.Require,
                    DiagnosticLevel = WistSsaDiagnosticLevel.Detailed
                }
            }
        });

        var result = wist.TryCompile<Func<double>>("2.0 + 3.0");
        var routeDiagnostics = result.OptimizationReport.Ssa.Diagnostics;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(routeDiagnostics, Is.Not.Empty);
            Assert.That(routeDiagnostics.Select(static diagnostic => diagnostic.Stage),
                Is.All.EqualTo("lowering"));
            Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Stage),
                Does.Contain("SSA Lowering"));
        });
    }
}
