using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDefinitionTests
{
    [Test]
    public void Constructor_RejectsEmptyName()
    {
        Assert.That(
            () => CreateDefinition(" "),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsWhitespaceBaseNameWhenProvided()
    {
        Assert.That(
            () => CreateDefinition("dialect", baseDialectName: " "),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_StoresOrderedRulesSnapshot()
    {
        var source = new List<OrderRule>
        {
            new(OrderRuleKind.Requires, "B", "A")
        };

        var definition = CreateDefinition("dialect", source);
        source.Add(new OrderRule(OrderRuleKind.After, "C", "D"));

        Assert.That(definition.OrderRules.Count, Is.EqualTo(1));
    }


    [Test]
    public void Constructor_AllowsMissingSecurityPolicy()
    {
        var definition = new DialectDefinition(
            "dialect",
            new ModulePolicy(),
            new BackendPolicy(),
            new IntrinsicPolicy(),
            new OptimizerPolicy(),
            null,
            new CapabilityPolicy());

        Assert.That(definition.SecurityPolicy, Is.Null);
    }

    [Test]
    public void Constructor_ExposesCapabilityLookup()
    {
        var capabilities = new CapabilityPolicy([
            new KeyValuePair<string, bool>("safe-interop", false)
        ]);

        var definition = new DialectDefinition(
            "dialect",
            new ModulePolicy(),
            new BackendPolicy(),
            new IntrinsicPolicy(),
            new OptimizerPolicy(),
            new SecurityPolicy(SecurityProfile.Restricted),
            capabilities);

        var found = definition.CapabilityPolicy.TryGetCapability("safe-interop", out var enabled);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(enabled, Is.False);
        });
    }

    private static DialectDefinition CreateDefinition(
        string name,
        IEnumerable<OrderRule>? orderRules = null,
        string? baseDialectName = null) =>
        new(
            name,
            new ModulePolicy(["Arithmetic"], ["Interop"]),
            new BackendPolicy([DialectBackendTarget.Cil], [DialectBackendTarget.Interpreter]),
            new IntrinsicPolicy(["add_i32"], ["unsafe_reflect"]),
            new OptimizerPolicy(["const_fold"], ["unsafe_inline"]),
            new SecurityPolicy(SecurityProfile.Restricted),
            new CapabilityPolicy([
                new KeyValuePair<string, bool>("supports-floats", true)
            ]),
            orderRules,
            "1.0",
            baseDialectName);
}