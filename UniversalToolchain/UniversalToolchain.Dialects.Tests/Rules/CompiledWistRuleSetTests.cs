using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Wist.Rules;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Rules;

public sealed class CompiledWistRuleSetTests
{
    private static readonly RuleTypeDescriptor NumberType = new("number");

    [Test]
    public void GetSchema_ReturnsRulesInDeterministicOrder()
    {
        var ruleSet = new CompiledWistRuleSet(
        [
            CreateRule("Total"),
            CreateRule("Discount")
        ]);

        var schema = ruleSet.GetSchema();

        Assert.That(schema.Rules.Select(static x => x.Name), Is.EqualTo(new[] { "Discount", "Total" }));
    }

    [Test]
    public void TryRun_WhenRuleIsUnknown_ReturnsStructuredDiagnostic()
    {
        var ruleSet = new CompiledWistRuleSet([CreateRule("Total")]);

        var result = ruleSet.TryRun("Missing", new Dictionary<string, object?>());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { ToolchainDiagnosticCodes.RuleUnknown }));
        });
    }

    [Test]
    public void TryRun_WhenRuleExists_DelegatesToCompiledRule()
    {
        var ruleSet = new CompiledWistRuleSet([CreateRule("Total", 42.0)]);

        var result = ruleSet.TryRun(
            "Total",
            new Dictionary<string, object?>
            {
                ["price"] = 42.0
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42.0));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    private static ICompiledRule CreateRule(string name, object? value = null)
    {
        return new FakeCompiledRule(
            new CompiledRuleDescriptor(
                name,
                [new RuleParameterDescriptor("price", NumberType)],
                NumberType),
            value);
    }

    private sealed class FakeCompiledRule : ICompiledRule
    {
        private readonly object? _value;

        public FakeCompiledRule(CompiledRuleDescriptor descriptor, object? value)
        {
            Descriptor = descriptor;
            _value = value;
        }

        public CompiledRuleDescriptor Descriptor { get; }

        public object? Run(IReadOnlyDictionary<string, object?> arguments)
        {
            return _value;
        }

        public RuleExecutionResult TryRun(IReadOnlyDictionary<string, object?> arguments)
        {
            return RuleExecutionResult.Success(_value);
        }
    }
}
