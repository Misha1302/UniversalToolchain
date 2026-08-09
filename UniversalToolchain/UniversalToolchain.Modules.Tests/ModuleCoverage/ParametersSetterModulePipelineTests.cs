using UniversalToolchain.Wist;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ParametersSetterModulePipelineTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules.Concat(["ParametersSetter"]).ToArray();

    [Test]
    public void ParametersSetter_SingleParameter_CanBeReadByProgram()
    {
        using var h = new ModulePipelineTestHelper();
        using var engine = CreateEngine(h);
        var value = engine.Evaluate<object?>("arg1", new Dictionary<string, object?> { ["arg1"] = 7.0 });
        Assert.That(ModulePipelineTestHelper.AsNumber(value), Is.EqualTo(7));
    }

    [Test]
    public void ParametersSetter_MultipleParameters_CanBeCombinedInExpression()
    {
        using var h = new ModulePipelineTestHelper();
        using var engine = CreateEngine(h);
        var value = engine.Evaluate<object?>("a + b", new Dictionary<string, object?>
        {
            ["a"] = 2.0,
            ["b"] = 3.0
        });
        Assert.That(ModulePipelineTestHelper.AsNumber(value), Is.EqualTo(5));
    }

    [Test]
    public void ParametersSetter_MissingRequiredParameter_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        using var engine = CreateEngine(h);
        var ex = Assert.Catch(() => engine.Evaluate<object?>("a + 1", new Dictionary<string, object?>()));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ParametersSetter_WrongParameterType_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        using var engine = CreateEngine(h);
        var ex = Assert.Catch(() => engine.Evaluate<object?>("a + 1", new Dictionary<string, object?> { ["a"] = "oops" }));
        Assert.That(ex!.Message, Is.Not.Empty);
    }

    [Test]
    public void ParametersSetter_ParametersDoNotLeakBetweenIndependentRuns()
    {
        using var h = new ModulePipelineTestHelper();
        using var engine = CreateEngine(h);
        var first = engine.Evaluate<object?>("a", new Dictionary<string, object?> { ["a"] = 2.0 });
        var ex = Assert.Catch(() => engine.Evaluate<object?>("a", new Dictionary<string, object?>()));
        Assert.That(ModulePipelineTestHelper.AsNumber(first), Is.EqualTo(2));
        Assert.That(ex, Is.Not.Null);
    }

    private static WistEngine CreateEngine(ModulePipelineTestHelper helper)
    {
        var dialect = helper.BuildDialectText("ParametersSetter", _modules, backends: ["interpreter"]);
        var options = WistEngineOptions.FromDialectText(dialect, "parameters-setter-tests");
        options.BackendId = "interpreter";
        options.AllowedAssemblies = [typeof(int).Assembly, typeof(ParametersSetterModulePipelineTests).Assembly];
        return WistEngine.Create(options);
    }
}
