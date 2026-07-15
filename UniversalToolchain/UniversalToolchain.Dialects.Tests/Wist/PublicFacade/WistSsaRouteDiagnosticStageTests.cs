using System.Reflection;
using UniversalToolchain.Ssa.Optimization;
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
    public void RouteException_WithEmissionStage_ProjectsFacadeStage()
    {
        var routeReport = new SsaRouteReport(
            SsaRoutePolicy.Require,
            "test-profile",
            usedSsa: false,
            fellBackToInput: false,
            inputAirInstructionCount: 3,
            outputAirInstructionCount: 3,
            diagnostics:
            [
                new SsaRouteDiagnostic(
                    "ssa.to-air.value-reuse.unsupported",
                    "Repeated value requires scheduling.",
                    "emission")
            ]);
        var routeException = new SsaRouteException(routeReport);
        var factoryType = typeof(WistEngine).Assembly.GetType(
            "UniversalToolchain.Wist.WistDiagnosticFactory",
            throwOnError: true)!;
        var fromException = factoryType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method => method.Name == "FromException");

        var projected = (IReadOnlyList<WistDiagnostic>)fromException.Invoke(
            null,
            [routeException, "Compilation", "<test>"])!;

        Assert.Multiple(() =>
        {
            Assert.That(projected, Has.Count.EqualTo(1));
            Assert.That(projected[0].Stage, Is.EqualTo("SSA Emission"));
            Assert.That(projected[0].Message, Does.StartWith("ssa.to-air.value-reuse.unsupported:"));
        });
    }
}
