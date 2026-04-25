using FunctionCallsModule;
using SafeMathFunctionsModule;
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

public sealed class FunctionCallsAndSafeMathCapabilityTests
{
    private static readonly FunctionTypeDescriptor NumberType = new("number");

    [Test]
    public void FunctionCallsModule_DeclaresOnlyGenericFunctionCallSyntax()
    {
        var catalog = new SelectedCapabilityCatalogBuilder().Build([typeof(FunctionCallsModuleImpl)]);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.LanguageFeatures.Select(static x => x.FeatureId.Value), Is.EqualTo(new[] { "FunctionCalls" }));
            Assert.That(catalog.BuiltinFunctionDescriptors, Is.Empty);
            Assert.That(catalog.BuiltinFunctionRuntimeBindings, Is.Empty);
            Assert.That(
                catalog.LanguageFeatures.Single().ProvidedSymbols.Select(static x => x.Name),
                Is.EqualTo(new[] { "function-call" }));
        });
    }

    [Test]
    public void SafeMathModule_OwnsSafeMathFunctionDescriptorsAndRuntimeBindings()
    {
        var catalog = new SelectedCapabilityCatalogBuilder().Build([typeof(SafeMathFunctionsModuleImpl)]);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.LanguageFeatures.Select(static x => x.FeatureId.Value), Is.EqualTo(new[] { "SafeMathFunctions" }));
            Assert.That(
                catalog.BuiltinFunctionDescriptors.Select(static x => x.Name),
                Is.EqualTo(new[] { "abs", "clamp", "max", "min" }));
            Assert.That(
                catalog.BuiltinFunctionRuntimeBindings.Select(static x => x.Signature.Name),
                Is.EqualTo(new[] { "abs", "clamp", "max", "min" }));
        });
    }

    [Test]
    public void SafeMathFunctionCatalog_ResolvesClampOnlyWhenSafeMathIsSelected()
    {
        var selectedPlan = CreateSelectedPlan(
            "SafeMathFunctions",
            typeof(SafeMathFunctionsModuleImpl));
        var selectedCatalog = new SelectedCapabilityCatalogBuilder().Build([typeof(SafeMathFunctionsModuleImpl)]);
        var functionCatalog = new BuiltinFunctionCatalog(selectedCatalog, selectedPlan);

        var resolution = functionCatalog.Resolve("clamp", [NumberType, NumberType, NumberType], "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(resolution.IsSuccess, Is.True);
            Assert.That(resolution.Descriptor?.FeatureId.Value, Is.EqualTo("SafeMathFunctions"));
            Assert.That(resolution.RuntimeBinding?.Method.DeclaringType, Is.EqualTo(typeof(SafeMathFunctions)));
            Assert.That(resolution.RuntimeBinding?.Method.Name, Is.EqualTo(nameof(SafeMathFunctions.Clamp)));
        });
    }

    [Test]
    public void SafeMathFunctionCatalog_DoesNotResolveClampWhenOnlyFunctionCallsIsSelected()
    {
        var selectedPlan = CreateSelectedPlan(
            "FunctionCalls",
            typeof(FunctionCallsModuleImpl));
        var selectedCatalog = new SelectedCapabilityCatalogBuilder().Build([typeof(FunctionCallsModuleImpl)]);
        var functionCatalog = new BuiltinFunctionCatalog(selectedCatalog, selectedPlan);

        var resolution = functionCatalog.Resolve("clamp", [NumberType, NumberType, NumberType], "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(resolution.IsSuccess, Is.False);
            Assert.That(resolution.Diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { ToolchainDiagnosticCodes.UnknownFunction }));
        });
    }

    [Test]
    public void SafeMathRuntimeHelpers_ReturnExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SafeMathFunctions.Abs(-7.0), Is.EqualTo(7.0));
            Assert.That(SafeMathFunctions.Clamp(15.0, 0.0, 10.0), Is.EqualTo(10.0));
            Assert.That(SafeMathFunctions.Max(3.0, 5.0), Is.EqualTo(5.0));
            Assert.That(SafeMathFunctions.Min(3.0, 5.0), Is.EqualTo(3.0));
        });
    }

    private static SelectedRuntimePlan CreateSelectedPlan(
        string moduleAlias,
        Type moduleImplementationType)
    {
        _ = moduleImplementationType;

        return new SelectedRuntimePlan(
            [CreateEntry(moduleAlias, $"{moduleAlias}-id")],
            [],
            [CreateEntry("interpreter", "interpreter-id", RuntimeComponentKind.Backend)],
            []);
    }

    private static RuntimeComponentManifestEntry CreateEntry(
        string alias,
        string id,
        RuntimeComponentKind kind = RuntimeComponentKind.FrontendModule)
    {
        return new RuntimeComponentManifestEntry(kind, alias, [], new RuntimeComponentId(id), "TestAssembly");
    }
}
