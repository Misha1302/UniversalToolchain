using AssemblyFinder;
using CommonExceptions;

namespace Tests.Native;

[TestFixture]
public class CSharpInteropResolutionAndNegativeContractsTests : TestBase
{
    [SetUp]
    public void SetUpMode()
    {
        SetArithmeticMode(ArithmeticMode.Native);
    }

    [Test]
    public void ExecuteCode_ShouldResolveSameUnambiguousCallShape_AcrossRepeatedRuns()
    {
        const string code = "System.Math.Abs(-5)";
        var expected = ExecuteCode<int>(code);

        for (var i = 0; i < 20; i++)
            Assert.That(ExecuteCode<int>(code), Is.EqualTo(expected));
    }

    [Test]
    public void MethodsFinder_ShouldFailPredictably_ForAmbiguousOverloadSignature()
    {
        var exception = Assert.Throws<AmbiguousMatchException>(() =>
            MethodsFinder.GetMethod($"{typeof(OverloadHost).FullName}.Pick", [typeof(int), typeof(int)]));

        Assert.That(exception!.Message, Does.Contain("Ambiguous match"));
    }

    [Test]
    public void ExecuteCode_ShouldRejectNonPublicInteropTarget()
    {
        var exception = Assert.Throws<ImportException>(() => ExecuteCode<int>($"{typeof(OverloadHost).FullName}.Hidden()"));

        Assert.That(exception!.Message, Does.Contain("not found").IgnoreCase);
    }

    [Test]
    public void ExecuteCode_ShouldRejectUnsupportedRefOutCallShape()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteCode<int>("System.Int32.TryParse(7)"));

        Assert.That(exception!.ParamName, Is.EqualTo("index"));
    }

    [Test]
    public void ExecuteCode_ShouldRejectNullCallShape_WhenCastContractCannotBeSatisfied()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ExecuteCode<int>("System.String.IsNullOrEmpty(null)"));

        Assert.That(exception!.Message, Does.Contain("Cannot cast").And.Contain("System.String"));
    }

    private sealed class OverloadHost
    {
        public static string Pick(int left, long right) => "int-long";
        public static string Pick(long left, int right) => "long-int";

        private static string Hidden() => "hidden";
    }
}
