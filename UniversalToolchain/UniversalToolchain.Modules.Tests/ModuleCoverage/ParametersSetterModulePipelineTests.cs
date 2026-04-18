using NumbersModule.Core;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ParametersSetterModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules.Concat(["ParametersSetter"]).ToArray();

    [Test]
    public void ParametersSetter_SingleParameter_CanBeReadByProgram()
    {
        using var h = new ModulePipelineTestHelper();
        using var host = h.CreateHost(Modules, backends: ["interpreter"]);
        var value = host.GetCore("interpreter").Run("arg1", new Dictionary<string, object> { ["arg1"] = 7 });
        Assert.That(ModulePipelineTestHelper.AsNumber(value), Is.EqualTo(7));
    }

    [Test]
    public void ParametersSetter_MultipleParameters_CanBeCombinedInExpression()
    {
        using var h = new ModulePipelineTestHelper();
        using var host = h.CreateHost(Modules, backends: ["interpreter"]);
        var value = host.GetCore("interpreter").Run("a + b", new Dictionary<string, object> { ["a"] = new RealNumberImpl(2), ["b"] = new RealNumberImpl(3) });
        Assert.That(ModulePipelineTestHelper.AsNumber(value), Is.EqualTo(5));
    }

    [Test]
    public void ParametersSetter_MissingRequiredParameter_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        using var host = h.CreateHost(Modules, backends: ["interpreter"]);
        var ex = Assert.Catch(() => host.GetCore("interpreter").Run("a + 1", new Dictionary<string, object>()));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ParametersSetter_WrongParameterType_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        using var host = h.CreateHost(Modules, backends: ["interpreter"]);
        var ex = Assert.Catch(() => host.GetCore("interpreter").Run("a + 1", new Dictionary<string, object> { ["a"] = "oops" }));
        Assert.That(ex!.Message, Is.Not.Empty);
    }

    [Test]
    public void ParametersSetter_ParametersDoNotLeakBetweenIndependentRuns()
    {
        using var h = new ModulePipelineTestHelper();
        using var host = h.CreateHost(Modules, backends: ["interpreter"]);
        var core = host.GetCore("interpreter");
        var first = core.Run("a", new Dictionary<string, object> { ["a"] = new RealNumberImpl(2) });
        var ex = Assert.Catch(() => core.Run("a", new Dictionary<string, object>()));
        Assert.That(ModulePipelineTestHelper.AsNumber(first), Is.EqualTo(2));
        Assert.That(ex, Is.Not.Null);
    }
}
