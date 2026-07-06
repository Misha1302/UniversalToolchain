using FunctionCallsModule;
using SafeMathFunctionsModule;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

public sealed class FunctionCallPlannerTests
{
    private static readonly FunctionTypeDescriptor Number = new("number");
    private static readonly LanguageFeatureId TestFeature = new("test");

    [Test]
    public void TryPlan_SafeMathRuntimeBinding_SelectsExactRuntimeMethod()
    {
        var planner = new FunctionCallPlanner(new SafeMathFunctionsCapabilityProvider().GetRuntimeBindings());

        var result = planner.TryPlan("clamp", [typeof(double), typeof(double), typeof(double)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Plan!.Binding.Method.Name, Is.EqualTo(nameof(SafeMathFunctions.Clamp)));
            Assert.That(result.Plan.AdapterCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryPlan_MissingFunctionAndArityReturnStableDiagnostics()
    {
        var planner = new FunctionCallPlanner(new SafeMathFunctionsCapabilityProvider().GetRuntimeBindings());

        var missing = planner.TryPlan("missing", [typeof(double)]);
        var wrongArity = planner.TryPlan("clamp", [typeof(double)]);

        Assert.Multiple(() =>
        {
            Assert.That(missing.IsSuccess, Is.False);
            Assert.That(missing.DiagnosticCode, Is.EqualTo("F001"));
            Assert.That(wrongArity.IsSuccess, Is.False);
            Assert.That(wrongArity.DiagnosticCode, Is.EqualTo("F002"));
        });
    }

    [Test]
    public void TryPlan_AmbiguousRuntimeBindingsReturnDiagnostic()
    {
        var planner = new FunctionCallPlanner(
        [
            Binding("ambiguous", nameof(AmbiguousA)),
            Binding("ambiguous", nameof(AmbiguousB))
        ]);

        var result = planner.TryPlan("ambiguous", [typeof(double)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.DiagnosticCode, Is.EqualTo("F004"));
            Assert.That(result.DiagnosticMessage, Does.Contain("multiple runtime bindings"));
        });
    }

    public static double AmbiguousA(double value) => value;

    public static double AmbiguousB(double value) => value;

    private static BuiltinFunctionRuntimeBinding Binding(string name, string methodName)
    {
        return new BuiltinFunctionRuntimeBinding(
            new BuiltinFunctionSignature(name, [Number]),
            Number,
            TestFeature,
            typeof(FunctionCallPlannerTests).GetMethod(methodName)!,
            ["interpreter"]);
    }
}
