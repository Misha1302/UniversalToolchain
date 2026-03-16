namespace Tests.Core;

[TestFixture]
public class BinderTests
{
    [Test]
    public void Should_BindExternalVariable_When_ExternalVariableExists()
    {
        var binder = new Binder([
            new ExternalBinding { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable }
        ]);

        var result = binder.Bind(CreateVariableNode("x"));

        Assert.That(result, Is.TypeOf<BoundExternalReference>());
        Assert.That(((BoundExternalReference)result).Symbol, Is.TypeOf<ExternalVariableSymbol>());
    }

    [Test]
    public void Should_BindExternalConstant_When_ExternalConstantExists()
    {
        var binder = new Binder([
            new ExternalBinding { Name = "pi", Type = typeof(double), Kind = ExternalBindingKind.Constant }
        ]);

        var result = binder.Bind(CreateVariableNode("pi"));

        Assert.That(result, Is.TypeOf<BoundExternalReference>());
        Assert.That(((BoundExternalReference)result).Symbol, Is.TypeOf<ExternalConstantSymbol>());
    }

    [Test]
    public void Should_CreateLocalReference_When_VariableDefinitionIsEncountered()
    {
        var binder = new Binder([]);

        var result = binder.Bind(CreateVariableNode("v", ["VariableDefinition"]));

        Assert.That(result, Is.TypeOf<BoundLocalReference>());
    }

    [Test]
    public void Should_ShadowExternal_When_LocalDefinitionWithSameNameExists()
    {
        var binder = new Binder([
            new ExternalBinding { Name = "v", Type = typeof(int), Kind = ExternalBindingKind.Variable }
        ]);

        var root = CreateNode("Root", "", [
            CreateVariableNode("v", ["VariableDefinition"]),
            CreateVariableNode("v")
        ]);

        var result = binder.Bind(root);

        Assert.That(result[1], Is.TypeOf<BoundLocalReference>());
    }

    [Test]
    public void Should_InferObjectLocal_When_VariableIsUnknown()
    {
        var binder = new Binder([]);

        var result = binder.Bind(CreateVariableNode("newLocal"));

        Assert.That(((BoundLocalReference)result).Symbol.Type, Is.EqualTo(typeof(object)));
    }

    [Test]
    public void Should_ResolveClrType_When_VariableDefinitionContainsTypeTag()
    {
        var binder = new Binder([]);
        var variable = CreateVariableNode(
            "typed",
            ["VariableDefinition", "VariableDefinitionWithType"],
            [CreateNode("Identifier", typeof(int).FullName!)]);

        var result = binder.Bind(variable);

        Assert.That(((BoundLocalReference)result).Symbol.Type, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void Should_FallbackToObject_When_VariableDefinitionTypeIsInvalid()
    {
        var binder = new Binder([]);
        var variable = CreateVariableNode(
            "typed",
            ["VariableDefinition", "VariableDefinitionWithType"],
            [CreateNode("Identifier", "Type.That.Does.Not.Exist")]);

        var result = binder.Bind(variable);

        Assert.That(((BoundLocalReference)result).Symbol.Type, Is.EqualTo(typeof(object)));
    }

    [Test]
    public void Should_RewriteChildrenRecursively_When_VariablesAreNested()
    {
        var binder = new Binder([]);
        var root = CreateNode("Root", "", [CreateNode("Scope", "", [CreateVariableNode("nested")])]);

        var result = binder.Bind(root);

        Assert.That(result[0][0], Is.TypeOf<BoundLocalReference>());
    }

    [Test]
    public void Should_UseConsistentSymbol_When_MultipleReferencesPointToSameLocal()
    {
        var binder = new Binder([]);
        var root = CreateNode("Root", "", [
            CreateVariableNode("a", ["VariableDefinition"]),
            CreateVariableNode("a"),
            CreateVariableNode("a")
        ]);

        var result = binder.Bind(root);

        var first = (BoundLocalReference)result[0];
        var second = (BoundLocalReference)result[1];
        var third = (BoundLocalReference)result[2];

        Assert.That(ReferenceEquals(first.Symbol, second.Symbol), Is.True);
        Assert.That(ReferenceEquals(second.Symbol, third.Symbol), Is.True);
    }

    [Test]
    public void Should_BindMixedLocalsAndExternals_When_AstContainsBothKinds()
    {
        var binder = new Binder([
            new ExternalBinding { Name = "ext", Type = typeof(string), Kind = ExternalBindingKind.Variable }
        ]);

        var root = CreateNode("Root", "", [
            CreateVariableNode("local", ["VariableDefinition"]),
            CreateVariableNode("ext"),
            CreateVariableNode("local")
        ]);

        var result = binder.Bind(root);

        Assert.That(result[0], Is.TypeOf<BoundLocalReference>());
        Assert.That(result[1], Is.TypeOf<BoundExternalReference>());
        Assert.That(result[2], Is.TypeOf<BoundLocalReference>());
    }


    [Test]
    public void Constructor_WithUnsupportedExternalBindingKind_ShouldThrowInvalidOperationException()
    {
        var invalidKind = (ExternalBindingKind)999;

        var ex = Assert.Throws<InvalidOperationException>(() => new Binder([
            new ExternalBinding { Name = "bad", Type = typeof(int), Kind = invalidKind }
        ]));

        Assert.That(ex, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex!.Message, Does.Contain("Unsupported external binding kind"));
    }

    private static AstNode CreateVariableNode(string name, string[]? tags = null, List<AstNode>? children = null)
    {
        var node = CreateNode("Variable", name, children ?? []);
        foreach (var tag in tags ?? [])
            node.AddTag(tag);
        return node;
    }

    private static AstNode CreateNode(string nodeType, string text, List<AstNode>? children = null)
    {
        var lexeme = new LexemeValue(text, null, -1, null);
        return new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType), lexeme, children ?? []);
    }
}