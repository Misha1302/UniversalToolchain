using BasicTypesExtensions;

namespace Tests.Internal;

[TestFixture]
public sealed class RemediationCoreRegressionTests
{
    [Test]
    public void ExtensibleEnum_IdentityIsNameBasedAndIndependentOfRegistrationOrder()
    {
        var firstOrder = new ExtensibleEnumCatalog<DemoTag>();
        var firstA = firstOrder.Register("a");
        var firstB = firstOrder.Register("b");
        var reverseOrder = new ExtensibleEnumCatalog<DemoTag>();
        var secondB = reverseOrder.Register("b");
        var secondA = reverseOrder.Register("a");

        Assert.Multiple(() =>
        {
            Assert.That(firstA, Is.EqualTo(secondA));
            Assert.That(firstB, Is.EqualTo(secondB));
            Assert.That(firstA.GetHashCode(), Is.EqualTo(secondA.GetHashCode()));
            Assert.That(ExtensibleEnum<DemoTag>.CreateOrGet("a"), Is.EqualTo(firstA));
        });
    }

    [Test]
    public void ExtensibleEnumCatalog_IsInstanceScopedRejectsDuplicatesAndFreezesDeterministically()
    {
        var first = new ExtensibleEnumCatalog<DemoTag>();
        var second = new ExtensibleEnumCatalog<DemoTag>();
        first.Register("z");
        first.Register("a");
        second.Register("other");

        Assert.Multiple(() =>
        {
            Assert.Throws<KeyNotFoundException>(() => second.Get("z"));
            Assert.Throws<InvalidOperationException>(() => first.Register("z"));
            Assert.That(first.Freeze().Select(static value => value.GetName()), Is.EqualTo(new[] { "a", "z" }));
            Assert.That(first.IsFrozen, Is.True);
            Assert.Throws<InvalidOperationException>(() => first.GetOrAdd("after-freeze"));
        });
    }

    [Test]
    public void RuntimeProviderActivation_RejectsAmbiguousAndUnsupportedConstructors()
    {
        var environment = new ExecutionEnvironment(
            [],
            allowedRuntimeProviderTypes: [typeof(AmbiguousProvider), typeof(ReverseAmbiguousProvider), typeof(UnsupportedProvider)]);

        var first = Assert.Throws<InvalidOperationException>(() => environment.GetRequiredProvider(typeof(AmbiguousProvider)));
        var reverse = Assert.Throws<InvalidOperationException>(() => environment.GetRequiredProvider(typeof(ReverseAmbiguousProvider)));
        var unsupported = Assert.Throws<InvalidOperationException>(() => environment.GetRequiredProvider(typeof(UnsupportedProvider)));

        Assert.Multiple(() =>
        {
            Assert.That(first!.Message, Does.Contain("ambiguous supported constructors"));
            Assert.That(reverse!.Message, Does.Contain("ambiguous supported constructors"));
            Assert.That(unsupported!.Message, Does.Contain("exactly one supported public constructor"));
        });
    }

    [Test]
    public void RuntimeProviderActivation_AcceptsEachExactSupportedSignature()
    {
        var environment = new ExecutionEnvironment(
            [],
            allowedRuntimeProviderTypes: [typeof(ParameterlessProvider), typeof(ContextStoreProvider), typeof(EnvironmentProvider)]);

        Assert.Multiple(() =>
        {
            Assert.That(environment.GetRequiredProvider(typeof(ParameterlessProvider)), Is.TypeOf<ParameterlessProvider>());
            Assert.That(environment.GetRequiredProvider(typeof(ContextStoreProvider)), Is.TypeOf<ContextStoreProvider>());
            Assert.That(environment.GetRequiredProvider(typeof(EnvironmentProvider)), Is.TypeOf<EnvironmentProvider>());
        });
    }

    [TestCase("bad", typeof(int), RuntimeValueConversionFailureKind.InvalidFormat)]
    [TestCase(long.MaxValue, typeof(int), RuntimeValueConversionFailureKind.Overflow)]
    [TestCase(1.5d, typeof(int), RuntimeValueConversionFailureKind.PrecisionLoss)]
    [TestCase(null, typeof(int), RuntimeValueConversionFailureKind.NullabilityViolation)]
    public void RuntimeValueConversion_ClassifiesFailures(
        object? value,
        Type targetType,
        RuntimeValueConversionFailureKind expectedKind)
    {
        var exception = Assert.Throws<RuntimeValueConversionException>(() =>
            RuntimeValueConversion.Convert(value, targetType));

        Assert.That(exception!.FailureKind, Is.EqualTo(expectedKind));
    }

    [Test]
    public void RuntimeValueConversion_ClassifiesUnsupportedConversion()
    {
        var exception = Assert.Throws<RuntimeValueConversionException>(() =>
            RuntimeValueConversion.Convert(new Version(1, 0), typeof(DateTime)));

        Assert.That(exception!.FailureKind, Is.EqualTo(RuntimeValueConversionFailureKind.UnsupportedConversion));
    }

    [Test]
    public void SetAndList_PreservesUniquenessAcrossDuplicateRemoveAndReAdd()
    {
        var values = new SetAndList<string>();
        values.Add("a");
        values.Add("a");
        values.Add("b");

        Assert.Multiple(() =>
        {
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Snapshot(), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(values.IndexOf("a"), Is.EqualTo(0));
            Assert.That(values.IndexOf("b"), Is.EqualTo(1));
        });

        Assert.That(values.Remove("a"), Is.True);
        values.Add("a");

        Assert.Multiple(() =>
        {
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Snapshot(), Is.EqualTo(new[] { "b", "a" }));
            Assert.That(values.IndexOf("b"), Is.EqualTo(0));
            Assert.That(values.IndexOf("a"), Is.EqualTo(1));
        });
    }

    private sealed class DemoTag;

    private sealed class AmbiguousProvider
    {
        public AmbiguousProvider(IRuntimeContextStore contextStore) { }
        public AmbiguousProvider(IExecutionEnvironment environment) { }
    }

    private sealed class ReverseAmbiguousProvider
    {
        public ReverseAmbiguousProvider(IExecutionEnvironment environment) { }
        public ReverseAmbiguousProvider(IRuntimeContextStore contextStore) { }
    }

    private sealed class UnsupportedProvider
    {
        public UnsupportedProvider(string value) { }
    }

    private sealed class ParameterlessProvider;

    private sealed class ContextStoreProvider
    {
        public ContextStoreProvider(IRuntimeContextStore contextStore) => _ = contextStore;
    }

    private sealed class EnvironmentProvider
    {
        public EnvironmentProvider(IExecutionEnvironment environment) => _ = environment;
    }
}
