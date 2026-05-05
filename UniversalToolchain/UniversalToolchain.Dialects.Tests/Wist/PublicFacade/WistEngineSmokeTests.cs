using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistEngineSmokeTests
{
    [Test]
    public void Evaluate_WithAnonymousArguments_ReturnsExpectedDouble()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var result = wist.Evaluate<double>(
            "price * 0.9 + fee",
            new
            {
                price = 100.0d,
                fee = 5.0d
            });

        Assert.That(result, Is.EqualTo(95.0d).Within(1e-9));
    }

    [Test]
    public void CompileFunc_TwoArguments_ReturnsTypedFastFunction()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var formula = wist.CompileFunc<double, double, double>(
            "price * 0.9 + fee",
            "price",
            "fee");

        var result = formula.Invoke(100.0d, 5.0d);

        Assert.That(result, Is.EqualTo(95.0d).Within(1e-9));
    }

    [Test]
    public void CompileFunc_ThreeArguments_ReturnsTypedFastFunction()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var formula = wist.CompileFunc<double, double, double, double>(
            "A + B * C / 5.0",
            "A",
            "B",
            "C");

        var result = formula.Invoke(10.0d, 20.0d, 30.0d);

        Assert.That(result, Is.EqualTo(130.0d).Within(1e-9));
    }

    [Test]
    public void CompileFunc_RepeatedInvoke_ReusesCompiledFunction()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var formula = wist.CompileFunc<double, double, double>(
            "price * 0.9 + fee",
            "price",
            "fee");

        var first = formula.Invoke(100.0d, 5.0d);
        var second = formula.Invoke(200.0d, 10.0d);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(95.0d).Within(1e-9));
            Assert.That(second, Is.EqualTo(190.0d).Within(1e-9));
        });
    }

    [Test]
    public void Validate_InvalidFormula_ReturnsFailureWithoutThrowing()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var result = wist.Validate(
            """
            let discount = 0.9
            price * discount + fee
            """,
            new
            {
                price = 100.0d,
                fee = 5.0d
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.Exception, Is.Not.Null);
        });
    }
}