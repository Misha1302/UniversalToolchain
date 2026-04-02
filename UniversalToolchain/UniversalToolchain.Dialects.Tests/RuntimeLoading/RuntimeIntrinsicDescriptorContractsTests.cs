using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class RuntimeIntrinsicDescriptorContractsTests
{
    [Test]
    public void Ctor_NullAliases_ProducesEmptyAliasList()
    {
        var descriptor = new RuntimeIntrinsicDescriptor("intrinsic.sample", new DialectBackendId("compiler"), null);

        Assert.That(descriptor.Aliases, Is.Empty);
    }

    [Test]
    public void Ctor_Aliases_AreSortedAndDeduplicated()
    {
        var descriptor = new RuntimeIntrinsicDescriptor(
            "intrinsic.sample",
            new DialectBackendId("compiler"),
            ["beta", "alpha", "beta", "intrinsic.sample"]);

        Assert.That(descriptor.Aliases, Is.EqualTo(new[] { "alpha", "beta" }));
    }

    [Test]
    public void Ctor_BlankAlias_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new RuntimeIntrinsicDescriptor("intrinsic.sample", new DialectBackendId("compiler"), [" "]));
    }

    [Test]
    public void Ctor_BlankCanonicalId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new RuntimeIntrinsicDescriptor(" ", new DialectBackendId("compiler"), ["alias"]));
    }

    [Test]
    public void AppliesTo_ReturnsExpectedResult_ForMatchingAndMismatchingBackends()
    {
        var descriptor = new RuntimeIntrinsicDescriptor("intrinsic.sample", new DialectBackendId("compiler"));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.AppliesTo(new DialectBackendId("compiler")), Is.True);
            Assert.That(descriptor.AppliesTo(new DialectBackendId("interpreter")), Is.False);
        });
    }
}
