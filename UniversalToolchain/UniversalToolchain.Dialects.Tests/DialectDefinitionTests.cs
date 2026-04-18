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
    public void Constructor_StoresImmutableDeterministicExtensionSnapshot()
    {
        var source = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["z-extension"] = 2,
            ["a-extension"] = "value"
        };

        var definition = CreateDefinition("dialect", extensions: source);
        source["new-extension"] = 3;

        Assert.Multiple(() =>
        {
            Assert.That(definition.Extensions.Keys, Is.EqualTo(new[] { "a-extension", "z-extension" }));
            Assert.That(definition.Extensions["a-extension"], Is.EqualTo("value"));
            Assert.That(definition.Extensions.ContainsKey("new-extension"), Is.False);
            Assert.That(
                () => ((IDictionary<string, object>)definition.Extensions).Add("late", 4),
                Throws.TypeOf<NotSupportedException>());
        });
    }

    [Test]
    public void Constructor_RejectsDuplicateExtensionKeys()
    {
        Assert.That(
            () => CreateDefinition(
                "dialect",
                extensions:
                [
                    new KeyValuePair<string, object>("custom", 1),
                    new KeyValuePair<string, object>("custom", 2)
                ]),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsInvalidExtensionEntries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CreateDefinition("dialect", extensions: [new KeyValuePair<string, object>(null!, 1)]),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => CreateDefinition("dialect", extensions: [new KeyValuePair<string, object>(" ", 1)]),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => CreateDefinition("dialect", extensions: [new KeyValuePair<string, object>("custom", null!)]),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Constructor_StoresCustomExtensionWithoutChangingTypedPolicies()
    {
        var definition = CreateDefinition(
            "dialect",
            extensions: [new KeyValuePair<string, object>("future.semantic", new[] { "custom" })]);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Extensions["future.semantic"], Is.EqualTo(new[] { "custom" }));
            Assert.That(definition.ModulePolicy.IncludedModules, Is.EqualTo(new[] { "Arithmetic" }));
            Assert.That(definition.BackendPolicy.EnabledBackends, Is.EqualTo(new[] { TestBackendIds.Cil }));
            Assert.That(definition.CapabilityPolicy.Capabilities.Keys, Is.EqualTo(new[] { "supports-floats" }));
        });
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
        string? baseDialectName = null,
        IEnumerable<KeyValuePair<string, object>>? extensions = null) =>
        new(
            name,
            new ModulePolicy(["Arithmetic"], ["Interop"]),
            new BackendPolicy([TestBackendIds.Cil], [TestBackendIds.Interpreter]),
            new IntrinsicPolicy(["add_i32"], ["unsafe_reflect"]),
            new OptimizerPolicy(["const_fold"], ["unsafe_inline"]),
            new SecurityPolicy(SecurityProfile.Restricted),
            new CapabilityPolicy([
                new KeyValuePair<string, bool>("supports-floats", true)
            ]),
            orderRules,
            "1.0",
            baseDialectName,
            extensions);
}