using NumbersModule.Core;
using NumbersModule.Module;
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Wist.Rules;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Rules;

public sealed class WistRuleRuntimeBindingTests
{
    [Test]
    public void RuntimeTypeResolver_UsesProvidedBindings()
    {
        var resolver = new WistRuleRuntimeTypeResolver(
        [
            new RuleRuntimeTypeBinding(
                new RuleTypeDescriptor("custom"),
                typeof(string),
                new PassThroughConverter())
        ]);

        var resolved = resolver.TryResolve(new RuleTypeDescriptor("custom"), out var runtimeType);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.True);
            Assert.That(runtimeType, Is.EqualTo(typeof(string)));
        });
    }

    [Test]
    public void RuleArgumentBinder_DelegatesRuntimeConversionToBindingConverter()
    {
        var converter = new TrackingConverter("adapted-value");
        var resolver = new WistRuleRuntimeTypeResolver(
        [
            new RuleRuntimeTypeBinding(
                new RuleTypeDescriptor("custom"),
                typeof(string),
                converter)
        ]);
        var adapter = new WistRuleRuntimeValueAdapter(resolver);
        var binder = new WistRuleArgumentBinder(adapter);

        var descriptor = new CompiledRuleDescriptor(
            "CustomRule",
            [new RuleParameterDescriptor("input", new RuleTypeDescriptor("custom"))],
            new RuleTypeDescriptor("custom"));

        var result = binder.Bind(
            descriptor,
            new Dictionary<string, object?>
            {
                ["input"] = 123
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(converter.WasCalled, Is.True);
            Assert.That(result.RuntimeArguments["input"], Is.EqualTo("adapted-value"));
        });
    }

    [Test]
    public void SelectedCapabilityCatalog_ContainsNumberRuleRuntimeBinding()
    {
        var catalog = new SelectedCapabilityCatalogBuilder().Build([typeof(NumbersModuleImpl)]);

        var binding = catalog.RuleRuntimeTypeBindings.Single(static x => x.RuleType.Name == "number");

        Assert.That(binding.RuntimeType, Is.EqualTo(typeof(RealNumberImpl)));
    }

    private sealed class PassThroughConverter : IRuleRuntimeValueConverter
    {
        public bool TryConvert(
            object? value,
            out object? runtimeValue,
            out ToolchainDiagnostic? diagnostic,
            string argumentName,
            string ruleName,
            RuleTypeDescriptor expectedType)
        {
            runtimeValue = value;
            diagnostic = null;
            return true;
        }
    }

    private sealed class TrackingConverter : IRuleRuntimeValueConverter
    {
        private readonly object _result;

        public TrackingConverter(object result)
        {
            _result = result;
        }

        public bool WasCalled { get; private set; }

        public bool TryConvert(
            object? value,
            out object? runtimeValue,
            out ToolchainDiagnostic? diagnostic,
            string argumentName,
            string ruleName,
            RuleTypeDescriptor expectedType)
        {
            WasCalled = true;
            runtimeValue = _result;
            diagnostic = null;
            return true;
        }
    }
}
