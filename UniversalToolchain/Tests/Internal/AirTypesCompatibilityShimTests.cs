using DotnetAirHelper;
using Tests.Infrastructure;

namespace Tests.Internal;

[TestFixture]
public sealed class AirTypesCompatibilityShimTests
{
    [Test]
    public void ProcessTypesIntrinsic_WithoutCustomOverride_DelegatesToSharedProcessor()
    {
        using var _ = GlobalTestStateScope.Create();

        var stack = new List<Type> { typeof(double), typeof(double) };
        var instruction = new Instruction(UOpCode.Intrinsic, ["cmp_le_f64"]);

        AirTypes.ProcessTypesIntrinsic(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(bool) }));
    }

    [Test]
    public void ProcessTypesIntrinsic_WithCustomOverride_UsesRegisteredProcessor()
    {
        using var _ = GlobalTestStateScope.Create();

        AirTypes.TryRegisterIntrinsic("test_intrinsic", (_, stack) => stack.Add(typeof(string)));

        var stack = new List<Type>();
        var instruction = new Instruction(UOpCode.Intrinsic, ["test_intrinsic"]);

        AirTypes.ProcessTypesIntrinsic(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(string) }));
    }

    [Test]
    public void ResetToDefaultsForTests_RemovesPreviouslyRegisteredCustomOverrides()
    {
        AirTypes.TryRegisterIntrinsic("test_intrinsic", (_, stack) => stack.Add(typeof(string)));

        AirTypes.ResetToDefaultsForTests();

        var stack = new List<Type>();
        var instruction = new Instruction(UOpCode.Intrinsic, ["test_intrinsic"]);

        Assert.Throws<InvalidOperationException>(() => AirTypes.ProcessTypesIntrinsic(instruction, stack));
    }
}
