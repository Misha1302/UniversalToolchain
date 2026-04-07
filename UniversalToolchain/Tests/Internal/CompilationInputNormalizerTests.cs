namespace Tests.Internal;

[TestFixture]
public class CompilationInputNormalizerTests
{
    [Test]
    public void NormalizeDeclaredInput_ShouldPreserveOrderedDictionaryOrder()
    {
        var normalizer = new CompilationInputNormalizer();
        var declared = new OrderedDictionary<string, Type>
        {
            ["b"] = typeof(int),
            ["a"] = typeof(double)
        };

        var input = normalizer.NormalizeDeclaredInput("b + a", declared);

        Assert.That(input.ExternalBindings.Select(x => x.Name), Is.EqualTo(new[] { "b", "a" }));
        Assert.That(input.ExternalBindings.Select(x => x.Type), Is.EqualTo(new[] { typeof(int), typeof(double) }));
        Assert.That(input.ExternalBindings.All(x => x.Kind == ExternalBindingKind.Variable), Is.True);
    }

    [Test]
    public void NormalizeRuntimeInput_ShouldMapNamesTypesAndValues()
    {
        var normalizer = new CompilationInputNormalizer();
        var runtime = new Dictionary<string, object>
        {
            ["a"] = 5,
            ["b"] = 7
        };

        var input = normalizer.NormalizeRuntimeInput("a + b", runtime);

        Assert.That(input.ExternalBindings.Count, Is.EqualTo(2));
        Assert.That(input.ExternalBindings[0].Name, Is.EqualTo("a"));
        Assert.That(input.ExternalBindings[0].Type, Is.EqualTo(typeof(int)));
        Assert.That(input.ExternalBindings[0].Value, Is.EqualTo(5));
        Assert.That(input.ExternalBindings[1].Name, Is.EqualTo("b"));
        Assert.That(input.ExternalBindings[1].Value, Is.EqualTo(7));
    }

    [Test]
    public void NormalizeRuntimeInput_ShouldSupportNullDictionaryAsEmpty()
    {
        var normalizer = new CompilationInputNormalizer();

        var input = normalizer.NormalizeRuntimeInput("40 + 2");

        Assert.That(input.ExternalBindings, Is.Empty);
        Assert.That(input.SourceText, Is.EqualTo("40 + 2"));
        Assert.That(input.Options, Is.Not.Null);
    }

    [Test]
    public void NormalizeDeclaredInput_ShouldSupportNullDictionaryAsEmpty()
    {
        var normalizer = new CompilationInputNormalizer();

        var input = normalizer.NormalizeDeclaredInput("40 + 2");

        Assert.That(input.ExternalBindings, Is.Empty);
        Assert.That(input.SourceText, Is.EqualTo("40 + 2"));
        Assert.That(input.Options, Is.Not.Null);
    }

    [Test]
    public void NormalizeRuntimeInput_WithNullExternalValue_ShouldKeepNullAndMapTypeToObject()
    {
        var normalizer = new CompilationInputNormalizer();
        var runtime = new Dictionary<string, object> { ["x"] = null! };

        var input = normalizer.NormalizeRuntimeInput("x", runtime);

        Assert.That(input.ExternalBindings, Has.Count.EqualTo(1));
        Assert.That(input.ExternalBindings[0].Name, Is.EqualTo("x"));
        Assert.That(input.ExternalBindings[0].Type, Is.EqualTo(typeof(object)));
        Assert.That(input.ExternalBindings[0].Value, Is.Null);
        Assert.That(input.ExternalBindings[0].Kind, Is.EqualTo(ExternalBindingKind.Variable));
    }

    [Test]
    public void NormalizeDeclaredInput_ShouldPreserveDeclaredTypesExactly()
    {
        var normalizer = new CompilationInputNormalizer();
        var declared = new OrderedDictionary<string, Type>
        {
            ["d"] = typeof(decimal),
            ["o"] = typeof(object)
        };

        var input = normalizer.NormalizeDeclaredInput("d", declared);

        Assert.That(input.ExternalBindings.Select(x => x.Type), Is.EqualTo(new[] { typeof(decimal), typeof(object) }));
        Assert.That(input.ExternalBindings.All(x => x.Kind == ExternalBindingKind.Variable), Is.True);
    }

    [Test]
    public void ExternalBindingsFactory_FromRuntimeValues_ShouldMapNullTypeToObjectAndKeepNullValue()
    {
        var method = typeof(CompilationInputNormalizer).Assembly
            .GetType("BasicCore.Compilation.ExternalBindingsFactory")!
            .GetMethod("FromRuntimeValues", BindingFlags.Public | BindingFlags.Static)!;

        var runtime = new Dictionary<string, object> { ["x"] = null! };
        var result = (IReadOnlyList<ExternalBinding>)method.Invoke(null, [runtime])!;

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Type, Is.EqualTo(typeof(object)));
        Assert.That(result[0].Value, Is.Null);
        Assert.That(result[0].Kind, Is.EqualTo(ExternalBindingKind.Variable));
    }

    [Test]
    public void ExternalBindingsFactory_FromDeclaredTypes_ShouldPreserveDeclaredTypeAndVariableKind()
    {
        var method = typeof(CompilationInputNormalizer).Assembly
            .GetType("BasicCore.Compilation.ExternalBindingsFactory")!
            .GetMethod("FromDeclaredTypes", BindingFlags.Public | BindingFlags.Static)!;

        var declared = new OrderedDictionary<string, Type> { ["x"] = typeof(DateTime) };
        var result = (IReadOnlyList<ExternalBinding>)method.Invoke(null, [declared])!;

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Type, Is.EqualTo(typeof(DateTime)));
        Assert.That(result[0].Kind, Is.EqualTo(ExternalBindingKind.Variable));
        Assert.That(result[0].Value, Is.Null);
    }
}