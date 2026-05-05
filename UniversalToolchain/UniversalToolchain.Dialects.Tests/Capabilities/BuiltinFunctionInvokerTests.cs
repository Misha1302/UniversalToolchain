using SafeMathFunctionsModule;
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

public sealed class BuiltinFunctionInvokerTests
{
    private static readonly FunctionTypeDescriptor _numberType = new("number");

    [Test]
    public void Invoke_WhenBindingIsResolved_InvokesStaticRuntimeMethod()
    {
        var catalog = CreateFunctionCatalog();
        var resolution = catalog.Resolve("clamp", [_numberType, _numberType, _numberType], "interpreter");

        var invocation = new BuiltinFunctionInvoker().Invoke(resolution, [15.0, 0.0, 10.0]);

        Assert.Multiple(() =>
        {
            Assert.That(invocation.IsSuccess, Is.True);
            Assert.That(invocation.Value, Is.EqualTo(10.0));
            Assert.That(invocation.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Invoke_WhenRuntimeArgumentTypeIsWrong_ReturnsStructuredDiagnostic()
    {
        var catalog = CreateFunctionCatalog();
        var resolution = catalog.Resolve("round", [_numberType], "interpreter");

        var invocation = new BuiltinFunctionInvoker().Invoke(resolution, ["wrong"]);

        Assert.Multiple(() =>
        {
            Assert.That(invocation.IsSuccess, Is.False);
            Assert.That(invocation.Diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { ToolchainDiagnosticCodes.WrongFunctionArgumentType }));
        });
    }

    private static BuiltinFunctionCatalog CreateFunctionCatalog()
    {
        var selectedCatalog = new SelectedCapabilityCatalogBuilder().Build([typeof(SafeMathFunctionsModuleImpl)]);
        var selectedPlan = new SelectedRuntimePlan(
            [CreateEntry("SafeMathFunctions", "SafeMathFunctions-id")],
            [],
            [CreateEntry("interpreter", "interpreter-id", RuntimeComponentKind.Backend)],
            []);

        return new BuiltinFunctionCatalog(selectedCatalog, selectedPlan);
    }

    private static RuntimeComponentManifestEntry CreateEntry(string alias, string id, RuntimeComponentKind kind = RuntimeComponentKind.FrontendModule) =>
        new(kind, alias, [
        ], new RuntimeComponentId(id), "TestAssembly");
}