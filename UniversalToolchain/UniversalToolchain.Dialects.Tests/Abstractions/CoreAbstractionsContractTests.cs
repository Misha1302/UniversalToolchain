using System.Reflection;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ExpressionTyping.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Abstractions;

[TestFixture]
public sealed class CoreAbstractionsContractTests
{
    [Test]
    public void CapabilityProviderAttribute_StoresProviderType()
    {
        var attribute = new DialectCapabilityProviderAttribute(typeof(SampleLanguageFeatureDescriptorProvider));

        Assert.That(attribute.ProviderType, Is.EqualTo(typeof(SampleLanguageFeatureDescriptorProvider)));
    }

    [Test]
    public void LanguageFeatureDescriptor_CanDescribeFeature()
    {
        var descriptor = new LanguageFeatureDescriptor(
            new LanguageFeatureId("feature.functions"),
            "Function calls",
            LanguageFeatureKind.Syntax,
            ["runtime.syntax"],
            [new LanguageFeatureId("feature.base")],
            [
                new LanguageFeatureSymbolDescriptor(
                    "function-call",
                    LanguageFeatureSymbolKind.SyntaxForm,
                    "identifier(argument, ...)",
                    "Calls a provider-backed built-in function.")
            ],
            ["interpreter"],
            "Supports built-in function call syntax.");

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.FeatureId.Value, Is.EqualTo("feature.functions"));
            Assert.That(descriptor.Kind, Is.EqualTo(LanguageFeatureKind.Syntax));
            Assert.That(descriptor.ProvidedSymbols, Has.Count.EqualTo(1));
            Assert.That(descriptor.ProvidedSymbols[0].Kind, Is.EqualTo(LanguageFeatureSymbolKind.SyntaxForm));
        });
    }

    [Test]
    public void BuiltinFunctionDescriptor_CanDescribePureFunction()
    {
        var descriptor = new BuiltinFunctionDescriptor(
            "sum",
            new LanguageFeatureId("feature.functions"),
            [
                new FunctionParameterDescriptor("left", new FunctionTypeDescriptor("Number")),
                new FunctionParameterDescriptor("right", new FunctionTypeDescriptor("Number"))
            ],
            new FunctionTypeDescriptor("Number"),
            FunctionPurity.Pure,
            ["interpreter", "cil"]);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Name, Is.EqualTo("sum"));
            Assert.That(descriptor.Purity, Is.EqualTo(FunctionPurity.Pure));
            Assert.That(descriptor.Parameters, Has.Count.EqualTo(2));
            Assert.That(descriptor.ReturnType.Name, Is.EqualTo("Number"));
        });
    }

    [Test]
    public void BuiltinFunctionRuntimeBinding_CanDescribeStaticMethod()
    {
        var method = typeof(SampleBindings).GetMethod(nameof(SampleBindings.Combine), BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var binding = new BuiltinFunctionRuntimeBinding(
            new BuiltinFunctionSignature(
                "combine",
                [new FunctionTypeDescriptor("Text"), new FunctionTypeDescriptor("Text")]),
            new FunctionTypeDescriptor("Text"),
            new LanguageFeatureId("feature.functions"),
            method!,
            ["interpreter"]);

        Assert.Multiple(() =>
        {
            Assert.That(binding.Method, Is.EqualTo(method));
            Assert.That(binding.Signature.Name, Is.EqualTo("combine"));
            Assert.That(binding.Signature.ParameterTypes, Has.Count.EqualTo(2));
            Assert.That(binding.ReturnType.Name, Is.EqualTo("Text"));
        });
    }

    [Test]
    public void ToolchainDiagnostic_CanRepresentDiagnostic()
    {
        var diagnostic = new ToolchainDiagnostic(
            ToolchainDiagnosticCodes.TypeMismatch,
            ToolchainDiagnosticSeverity.Error,
            "The resolved type does not match the expected type.",
            new SourceSpan("input.dsl", 1, 1, 1, 5),
            [new ToolchainDiagnosticHint("Check the declared binding type.")]);

        var context = new ExpressionTypeResolutionContext(
            new Dictionary<string, ExpressionTypeDescriptor>(StringComparer.Ordinal)
            {
                ["value"] = new ExpressionTypeDescriptor("Number")
            },
            [diagnostic]);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(ToolchainDiagnosticCodes.TypeMismatch));
            Assert.That(diagnostic.Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Error));
            Assert.That(diagnostic.Span?.SourceName, Is.EqualTo("input.dsl"));
            Assert.That(diagnostic.Hints[0].Message, Does.Contain("binding type"));
            Assert.That(context.KnownBindings["value"].Name, Is.EqualTo("Number"));
        });
    }

    private sealed class SampleLanguageFeatureDescriptorProvider : ILanguageFeatureDescriptorProvider
    {
        public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
        {
            return [];
        }
    }

    private static class SampleBindings
    {
        public static string Combine(string left, string right)
        {
            return left + right;
        }
    }
}
