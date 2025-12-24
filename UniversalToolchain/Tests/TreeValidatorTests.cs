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