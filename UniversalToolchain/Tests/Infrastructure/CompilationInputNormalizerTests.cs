using BasicCore.Compilation;

namespace Tests.Infrastructure;

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

        var input = normalizer.NormalizeRuntimeInput("40 + 2", null);

        Assert.That(input.ExternalBindings, Is.Empty);
        Assert.That(input.SourceText, Is.EqualTo("40 + 2"));
        Assert.That(input.Options, Is.Not.Null);
    }

    [Test]
    public void NormalizeDeclaredInput_ShouldSupportNullDictionaryAsEmpty()
    {
        var normalizer = new CompilationInputNormalizer();

        var input = normalizer.NormalizeDeclaredInput("40 + 2", null);

        Assert.That(input.ExternalBindings, Is.Empty);
        Assert.That(input.SourceText, Is.EqualTo("40 + 2"));
        Assert.That(input.Options, Is.Not.Null);
    }
}
