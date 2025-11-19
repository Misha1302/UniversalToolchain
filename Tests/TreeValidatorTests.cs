// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicParser;
using BasicTypesExtensions;

namespace Tests;

[TestFixture]
public class TreeValidatorTests
{
    [Test]
    public void IsValidTree_WithValidTree_ReturnsTrue()
    {
        // Arrange
        var validator = new TreeValidator();
        var root = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null, new List<AstNode>());
        var child = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Number"), null, new List<AstNode>());
        root.Children.Add(child);

        // Act
        var isValid = validator.IsValidTree(root);

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void IsValidTree_WithUnknownNodeType_ReturnsFalse()
    {
        // Arrange
        var validator = new TreeValidator();
        var root = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null, new List<AstNode>());
        var child = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Unknown"), null, new List<AstNode>());
        root.Children.Add(child);

        // Act
        var isValid = validator.IsValidTree(root);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTree_WithNestedValidTree_ReturnsTrue()
    {
        // Arrange
        var validator = new TreeValidator();
        var root = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null, new List<AstNode>());
        var child = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null, new List<AstNode>());
        var grandchild = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Number"), null, new List<AstNode>());
        child.Children.Add(grandchild);
        root.Children.Add(child);

        // Act
        var isValid = validator.IsValidTree(root);

        // Assert
        Assert.That(isValid, Is.True);
    }
}