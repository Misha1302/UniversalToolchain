namespace Tests;

[TestFixture]
public class ExtensibleEnumTests
{
    [Test]
    public void CreateOrGet_SameName_ReturnsSameInstance()
    {
        var enum1 = ExtensibleEnum<object>.CreateOrGet("TestType");
        var enum2 = ExtensibleEnum<object>.CreateOrGet("TestType");


        Assert.That(enum1, Is.EqualTo(enum2));
        Assert.That(enum1.GetHashCode(), Is.EqualTo(enum2.GetHashCode()));
    }

    [Test]
    public void CreateOrGet_DifferentNames_ReturnsDifferentInstances()
    {
        var enum1 = ExtensibleEnum<object>.CreateOrGet("Type1");
        var enum2 = ExtensibleEnum<object>.CreateOrGet("Type2");


        Assert.That(enum1, Is.Not.EqualTo(enum2));
    }

    [Test]
    public void GetName_ReturnsCorrectName()
    {
        var enumValue = ExtensibleEnum<object>.CreateOrGet("TestName");


        var name = enumValue.GetName();


        Assert.That(name, Is.EqualTo("TestName"));
    }
}