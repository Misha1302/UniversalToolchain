// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicTypesExtensions;

namespace Tests;

[TestFixture]
public class ExtensibleEnumTests
{
    [Test]
    public void CreateOrGet_SameName_ReturnsSameInstance()
    {
        // Arrange & Act
        var enum1 = ExtensibleEnum<object>.CreateOrGet("TestType");
        var enum2 = ExtensibleEnum<object>.CreateOrGet("TestType");

        // Assert
        Assert.That(enum1, Is.EqualTo(enum2));
        Assert.That(enum1.GetHashCode(), Is.EqualTo(enum2.GetHashCode()));
    }

    [Test]
    public void CreateOrGet_DifferentNames_ReturnsDifferentInstances()
    {
        // Arrange & Act
        var enum1 = ExtensibleEnum<object>.CreateOrGet("Type1");
        var enum2 = ExtensibleEnum<object>.CreateOrGet("Type2");

        // Assert
        Assert.That(enum1, Is.Not.EqualTo(enum2));
    }

    [Test]
    public void GetName_ReturnsCorrectName()
    {
        // Arrange
        var enumValue = ExtensibleEnum<object>.CreateOrGet("TestName");

        // Act
        var name = enumValue.GetName();

        // Assert
        Assert.That(name, Is.EqualTo("TestName"));
    }
}