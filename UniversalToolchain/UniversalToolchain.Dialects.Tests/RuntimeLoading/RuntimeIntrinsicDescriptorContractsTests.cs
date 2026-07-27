using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class RuntimeIntrinsicDescriptorContractsTests
{
    [Test]
    public void Ctor_NullAliases_ProducesEmptyAliasList()
    {
        var descriptor = new RuntimeIntrinsicDescriptor("intrinsic.sample", new DialectBackendId("cil"));

        Assert.That(descriptor.Aliases, Is.Empty);
    }

    [Test]
    public void Ctor_Aliases_AreSortedAndDeduplicated()
    {
        var descriptor = new RuntimeIntrinsicDescriptor(
            "intrinsic.sample",
            new DialectBackendId("cil"),
            ["beta", "alpha", "beta", "intrinsic.sample"]);

        Assert.That(descriptor.Aliases, Is.EqualTo(new[] { "alpha", "beta" }));
    }

    [Test]
    public void Ctor_BlankAlias_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => { _ = new RuntimeIntrinsicDescriptor("intrinsic.sample", new DialectBackendId("cil"), [" "]); });
    }

    [Test]
    public void Ctor_BlankCanonicalId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => { _ = new RuntimeIntrinsicDescriptor(" ", new DialectBackendId("cil"), ["alias"]); });
    }

    [Test]
    public void AppliesTo_ReturnsExpectedResult_ForMatchingAndMismatchingBackends()
    {
        var descriptor = new RuntimeIntrinsicDescriptor("intrinsic.sample", new DialectBackendId("cil"));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.AppliesTo(new DialectBackendId("cil")), Is.True);
            Assert.That(descriptor.AppliesTo(new DialectBackendId("interpreter")), Is.False);
        });
    }
}