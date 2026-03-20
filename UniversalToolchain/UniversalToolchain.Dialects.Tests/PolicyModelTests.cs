namespace UniversalToolchain.Dialects.Tests;

public class PolicyModelTests
{
    [Test]
    public void ModulePolicy_RejectsOverlap()
    {
        Assert.That(
            () => new ModulePolicy(["Arithmetic"], ["Arithmetic"]),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void BackendPolicy_RejectsOverlap()
    {
        Assert.That(
            () => new BackendPolicy([TestBackendIds.Cil], [TestBackendIds.Cil]),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void IntrinsicPolicy_RejectsOverlap()
    {
        Assert.That(
            () => new IntrinsicPolicy(["load_dotnet"], ["load_dotnet"]),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void OptimizerPolicy_RejectsOverlap()
    {
        Assert.That(
            () => new OptimizerPolicy(["const_fold"], ["const_fold"]),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void OrderRule_RejectsSameSourceAndTarget()
    {
        Assert.That(
            () => new OrderRule(OrderRuleKind.Before, "A", "A"),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void SecurityPolicy_RejectsUndefinedEnum()
    {
        Assert.That(
            () => new SecurityPolicy((SecurityProfile)123),
            Throws.TypeOf<ArgumentException>());
    }
}