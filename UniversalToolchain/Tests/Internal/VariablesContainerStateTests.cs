using SettableGettableModule.Core;
using Tests.Infrastructure;

namespace Tests.Internal;

[TestFixture]
public class VariablesContainerStateTests
{
    [Test]
    public void Should_IsolateValuesByKey_When_UsingVariablesContainer()
    {
        using var _ = GlobalTestStateScope.Create();

        const string keyA = "iso-a";
        const string keyB = "iso-b";
        VariablesContainer<int>.Set(keyA, 42);
        VariablesContainer<int>.Set(keyB, 7);

        var valueA = VariablesContainer<int>.Get(keyA);
        var valueB = VariablesContainer<int>.Get(keyB);

        Assert.That(valueA, Is.EqualTo(42));
        Assert.That(valueB, Is.EqualTo(7));
    }

    [Test]
    public void VariablesContainer_Get_ShouldThrowClearException_WhenKeyIsMissing()
    {
        using var _ = GlobalTestStateScope.Create();

        const string missingKey = "missing-key";

        var exception = Assert.Throws<KeyNotFoundException>(() => VariablesContainer<int>.Get(missingKey));

        Assert.That(exception!.Message, Does.Contain(missingKey));
    }

    [Test]
    public void VariablesContainer_Get_ShouldReturnValue_WhenKeyExists()
    {
        using var _ = GlobalTestStateScope.Create();

        const string key = "existing-key";
        VariablesContainer<int>.Set(key, 123);

        var value = VariablesContainer<int>.Get(key);

        Assert.That(value, Is.EqualTo(123));
    }
}
