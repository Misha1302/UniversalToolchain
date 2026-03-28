using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests;

public class DiagnosticsAndPlanTests
{
    [Test]
    public void DialectDiagnostic_HasValueEqualitySemantics()
    {
        var left = new DialectDiagnostic("D100", "Message", DialectDiagnosticSeverity.Error);
        var right = new DialectDiagnostic("D100", "Message", DialectDiagnosticSeverity.Error);

        Assert.That(left, Is.EqualTo(right));
    }

    [Test]
    public void DialectValidationResult_IsInvalidWhenErrorExists()
    {
        var result = new DialectValidationResult([
            new DialectDiagnostic("D100", "Error", DialectDiagnosticSeverity.Error),
            new DialectDiagnostic("D101", "Warning", DialectDiagnosticSeverity.Warning)
        ]);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void DialectBuildPlan_CanBuildReflectsValidationState()
    {
        var result = new DialectValidationResult([
            new DialectDiagnostic("D200", "No error", DialectDiagnosticSeverity.Info)
        ]);

        var plan = new DialectBuildPlan(
            "dialect",
            "1.0",
            ["A", "B"],
            [TestBackendIds.Interpreter],
            [TestBackendIds.Cil],
            [new IntrinsicBuildDirective("add_i32", true, TestBackendIds.Any)],
            [new OptimizerBuildDirective("const_fold", true, TestBackendIds.Any)],
            SecurityProfile.Trusted,
            [new KeyValuePair<string, bool>("supports-floats", true)],
            result);

        Assert.That(plan.CanBuild, Is.True);
    }
}