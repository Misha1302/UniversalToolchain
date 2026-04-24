using UniversalToolchain.Dialects.Wist.Features;
using UniversalToolchain.Dialects.Wist.Functions;
using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Features.Core;
using UniversalToolchain.Functions.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Functions;

[TestFixture]
public sealed class WistBuiltinFunctionCatalogTests
{
    [Test]
    public void Resolve_UnknownFunction_ReturnsUnknownFunctionDiagnostic()
    {
        var catalog = CreateCatalog(CreateNumberFunctionDescriptor());

        var result = catalog.Resolve(
            "missing",
            [WistFunctionTypeDescriptors.Number],
            CreateExplanation(WistLanguageFeatureIds.StandardNumbers),
            "interpreter");

        AssertDiagnostic(result, RuleDiagnosticCodes.UnknownFunction);
    }

    [Test]
    public void Resolve_FunctionWithUnavailableFeature_ReturnsUnavailableDiagnostic()
    {
        var catalog = CreateCatalog(CreateNumberFunctionDescriptor());

        var result = catalog.Resolve(
            "round",
            [WistFunctionTypeDescriptors.Number],
            CreateExplanation(),
            "interpreter");

        AssertDiagnostic(result, RuleDiagnosticCodes.FunctionUnavailable);
    }

    [Test]
    public void Resolve_FunctionWithUnsupportedBackend_ReturnsBackendDiagnostic()
    {
        var catalog = CreateCatalog(CreateNumberFunctionDescriptor(["cil"]));

        var result = catalog.Resolve(
            "round",
            [WistFunctionTypeDescriptors.Number],
            CreateExplanation(WistLanguageFeatureIds.StandardNumbers),
            "interpreter");

        AssertDiagnostic(result, RuleDiagnosticCodes.FunctionUnsupportedBackend);
    }

    [Test]
    public void Resolve_WrongArgumentCount_ReturnsArgumentCountDiagnostic()
    {
        var catalog = CreateCatalog(CreateBinaryBoolFunctionDescriptor());

        var result = catalog.Resolve(
            "and",
            [WistFunctionTypeDescriptors.Bool],
            CreateExplanation(WistLanguageFeatureIds.BooleanLogic),
            "interpreter");

        AssertDiagnostic(result, RuleDiagnosticCodes.WrongFunctionArgumentCount);
    }

    [Test]
    public void Resolve_WrongArgumentType_ReturnsArgumentTypeDiagnostic()
    {
        var catalog = CreateCatalog(CreateBinaryBoolFunctionDescriptor());

        var result = catalog.Resolve(
            "and",
            [WistFunctionTypeDescriptors.Number, WistFunctionTypeDescriptors.Number],
            CreateExplanation(WistLanguageFeatureIds.BooleanLogic),
            "interpreter");

        AssertDiagnostic(result, RuleDiagnosticCodes.WrongFunctionArgumentType);
    }

    [Test]
    public void Resolve_ValidDescriptor_ReturnsSuccess()
    {
        var descriptor = CreateNumberFunctionDescriptor();
        var catalog = CreateCatalog(descriptor);

        var result = catalog.Resolve(
            "round",
            [WistFunctionTypeDescriptors.Number],
            CreateExplanation(WistLanguageFeatureIds.StandardNumbers),
            "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Descriptor, Is.EqualTo(descriptor));
            Assert.That(result.ReturnType, Is.EqualTo(WistFunctionTypeDescriptors.Number));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    private static void AssertDiagnostic(BuiltinFunctionResolution result, string expectedCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Descriptor, Is.Null);
            Assert.That(result.ReturnType, Is.Null);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { expectedCode }));
        });
    }

    private static WistBuiltinFunctionCatalog CreateCatalog(params BuiltinFunctionDescriptor[] descriptors)
    {
        return new WistBuiltinFunctionCatalog(descriptors);
    }

    private static DialectFeatureExplanation CreateExplanation(params LanguageFeatureId[] availableFeatureIds)
    {
        return new DialectFeatureExplanation(
            "TestDialect",
            availableFeatureIds.Select(CreateAvailableFeature).ToArray(),
            [],
            [],
            [new DialectFeatureBackendSupport("interpreter", availableFeatureIds)]);
    }

    private static AvailableLanguageFeature CreateAvailableFeature(LanguageFeatureId featureId)
    {
        return new AvailableLanguageFeature(
            new LanguageFeatureDescriptor(
                featureId,
                featureId.Value,
                LanguageFeatureKind.FunctionSet,
                [],
                [],
                [],
                ["interpreter", "cil"],
                featureId.Value + " description."));
    }

    private static BuiltinFunctionDescriptor CreateNumberFunctionDescriptor(IReadOnlyList<string>? backends = null)
    {
        return new BuiltinFunctionDescriptor(
            "round",
            WistLanguageFeatureIds.StandardNumbers,
            [new FunctionParameterDescriptor("value", WistFunctionTypeDescriptors.Number)],
            WistFunctionTypeDescriptors.Number,
            FunctionPurity.Pure,
            backends ?? ["interpreter", "cil"]);
    }

    private static BuiltinFunctionDescriptor CreateBinaryBoolFunctionDescriptor()
    {
        return new BuiltinFunctionDescriptor(
            "and",
            WistLanguageFeatureIds.BooleanLogic,
            [
                new FunctionParameterDescriptor("left", WistFunctionTypeDescriptors.Bool),
                new FunctionParameterDescriptor("right", WistFunctionTypeDescriptors.Bool)
            ],
            WistFunctionTypeDescriptors.Bool,
            FunctionPurity.Pure,
            ["interpreter", "cil"]);
    }
}
