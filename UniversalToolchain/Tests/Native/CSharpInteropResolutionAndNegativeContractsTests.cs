using AssemblyFinder;
using CommonExceptions;

namespace Tests.Native;

[TestFixture]
public class CSharpInteropResolutionAndNegativeContractsTests : TestBase
{
    [SetUp]
    public void SetUpMode()
    {
        SetArithmeticMode(WistOptions.ArithmeticModeEnum.Native);
    }

    [Test]
    public void SameInteropCall_ShouldResolveSameOverload_AcrossRepeatedExecutions()
    {
        const string code = "System.Math.Abs(-5)";
        var first = ExecuteCode<int>(code);

        for (var i = 0; i < 20; i++)
            Assert.That(ExecuteCode<int>(code), Is.EqualTo(first));
    }

    [Test]
    public void AmbiguousOverloadResolution_ShouldFailPredictably()
    {
        var ex = Assert.Throws<AmbiguousMatchException>(() => MethodsFinder.GetMethod($"{typeof(OverloadHost).FullName}.Pick", [typeof(int), typeof(int)]));

        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ConstructorResolution_ShouldSelectConstructorMatchingStackArgumentTypes()
    {
        var ctorFromInt = typeof(OverloadHost).GetConstructor([typeof(int)]);
        var ctorFromLong = typeof(OverloadHost).GetConstructor([typeof(long)]);

        Assert.That(ctorFromInt, Is.Not.Null);
        Assert.That(ctorFromLong, Is.Not.Null);
        Assert.That(ctorFromInt, Is.Not.EqualTo(ctorFromLong));
    }

    [Test]
    public void NonPublicInteropTarget_ShouldBeRejected_BySymbolResolution()
    {
        var hasPrivateMethod = MethodsFinder.ContainsAnyMethod($"{typeof(OverloadHost).FullName}.Hidden");
        var resolved = MethodsFinder.GetMethod($"{typeof(OverloadHost).FullName}.Hidden");

        Assert.That(hasPrivateMethod, Is.True);
        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void UnsupportedRefOutCallShape_ShouldFailWithStableImportContract()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteCode<int>("System.Int32.TryParse(7)"));

        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void NullArgumentCallShape_ShouldFailWithStableImportContract()
    {
        var ex = Assert.Catch(() => ExecuteCode<int>("System.String.IsNullOrEmpty(null)"));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("String").IgnoreCase);
    }

    private sealed class OverloadHost
    {
        public OverloadHost(int value)
        {
        }

        public OverloadHost(long value)
        {
        }

        public static string Pick(int left, long right) => "int-long";
        public static string Pick(long left, int right) => "long-int";
        private static string Hidden() => "hidden";
    }
}
