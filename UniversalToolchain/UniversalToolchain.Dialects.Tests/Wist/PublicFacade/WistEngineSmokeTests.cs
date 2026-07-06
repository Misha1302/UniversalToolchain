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
    public void Evaluate_WithExplicitPresetFactories_ReturnsExpectedDouble()
    {
        using var restrictedArithmetic = WistEngine.CreateRestrictedArithmetic();
        using var fullNativePreview = WistEngine.CreateFullNativePreview();

        var restrictedResult = restrictedArithmetic.Evaluate<double>(
            "price * 0.9 + fee",
            new { price = 100.0d, fee = 5.0d });
        var fullResult = fullNativePreview.Evaluate<double>(
            "price * 0.9 + fee",
            new { price = 100.0d, fee = 5.0d });

        Assert.Multiple(() =>
        {
            Assert.That(restrictedResult, Is.EqualTo(95.0d).Within(1e-9));
            Assert.That(fullResult, Is.EqualTo(95.0d).Within(1e-9));
        });
    }

    [Test]
    public void CompileFunc_OneArgument_ReturnsExpectedResult()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var formula = wist.CompileFunc<double, double>(
            "price * 0.9",
            "price");

        var result = formula.Invoke(100.0d);

        Assert.That(result, Is.EqualTo(90.0d).Within(1e-9));
    }

    [Test]
    public void CompileFunc_TwoArguments_ReturnsExpectedResult()
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
    public void CompileFunc_ThreeArguments_ReturnsExpectedResult()
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
    public void CompileFunc_InvokeRepeatedly_ReturnsStableResults()
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
    public void Compile_Delegate_ReturnsExpectedResultAndMetadata()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var program = wist.Compile<Func<double, double, double>>(
            "price * 0.9 + fee",
            "price",
            "fee");

        var result = program.CompiledDelegate(100.0d, 5.0d);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(95.0d).Within(1e-9));
            Assert.That(program.Metadata.Backend, Is.EqualTo("compiler"));
            Assert.That(program.Metadata.ParameterNames, Is.EqualTo(new[] { "price", "fee" }));
            Assert.That(program.Metadata.ParameterTypes, Is.EqualTo(new[] { typeof(double), typeof(double) }));
            Assert.That(program.Metadata.ReturnType, Is.EqualTo(typeof(double)));
        });
    }

    [Test]
    public void Compile_Delegate_InvokeRepeatedly_ReturnsStableResults()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var program = wist.Compile<Func<double, double, double>>(
            "price * 0.9 + fee",
            "price",
            "fee");

        var first = program.CompiledDelegate(100.0d, 5.0d);
        var second = program.CompiledDelegate(200.0d, 10.0d);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(95.0d).Within(1e-9));
            Assert.That(second, Is.EqualTo(190.0d).Within(1e-9));
        });
    }

    [Test]
    public void TryCompile_Delegate_WhenFormulaInvalid_ReturnsFailureWithoutThrowing()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var result = wist.TryCompile<Func<double, double>>(
            "price *",
            "price");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Program, Is.Null);
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Message, Is.Not.Empty);
        });
    }

    [Test]
    public void TryCompile_Delegate_WhenDelegateReturnsVoid_ReturnsFailureWithoutThrowing()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var result = wist.TryCompile<Action<double>>(
            "price * 0.9",
            "price");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Program, Is.Null);
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Message, Does.Contain("return a value"));
        });
    }

    [Test]
    public void Compile_Delegate_WhenParameterNamesAreDuplicated_FailsClearly()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var exception = Assert.Catch(
            () => wist.Compile<Func<double, double, double>>(
                "price * 0.9 + price",
                "price",
                "price"));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("Duplicate parameter name"));
        });
    }

    [Test]
    public void CompileFunc_WhenFormulaInvalid_FailsAtCompilation()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var exception = Assert.Catch(() => wist.CompileFunc<double, double>("price *", "price"));

        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    public void CompileFunc_WhenFormulaUsesUnsupportedSafeFormulaShape_FailsClearly()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var exception = Assert.Catch(
            () => wist.CompileFunc<double, double, double>(
                """
                let discount = 0.9
                price * discount + fee
                """,
                "price",
                "fee"));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception, Is.TypeOf<WistDialectFeatureException>());
            Assert.That(exception!.Message, Does.Contain("Feature 'let' is not enabled"));
            Assert.That(exception.Message, Does.Contain("SafeFormulas"));
            Assert.That(exception.Message, Does.Contain("Variables"));
        });
    }

    [Test]
    public void Validate_ValidFormulaWithSampleArguments_ReturnsSuccess()
    {
        using var wist = WistEngine.CreateSafeFormulas();

        var result = wist.Validate(
            "price * 0.9 + fee",
            new
            {
                price = 100.0d,
                fee = 5.0d
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Message, Is.Null);
            Assert.That(result.Exception, Is.Null);
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
            Assert.That(result.Message, Does.Contain("Feature 'let' is not enabled"));
            Assert.That(result.Message, Does.Contain("SafeFormulas"));
            Assert.That(result.Message, Does.Contain("Variables"));
            Assert.That(result.Exception, Is.TypeOf<WistDialectFeatureException>());
        });
    }
}
