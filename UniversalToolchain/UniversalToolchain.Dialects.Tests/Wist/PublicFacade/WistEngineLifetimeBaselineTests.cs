using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistEngineLifetimeBaselineTests
{
    [Test]
    public void Dispose_IsIdempotent()
    {
        var wist = WistEngine.CreateRestrictedArithmetic();

        Assert.DoesNotThrow(() =>
        {
            wist.Dispose();
            wist.Dispose();
        });
    }

    [Test]
    public void Evaluate_AfterDispose_ThrowsObjectDisposedException()
    {
        var wist = WistEngine.CreateRestrictedArithmetic();
        wist.Dispose();

        Assert.Throws<ObjectDisposedException>(() => wist.Evaluate<double>("1 + 2"));
    }

    [Test]
    public void Compile_AfterDispose_ThrowsObjectDisposedException()
    {
        var wist = WistEngine.CreateRestrictedArithmetic();
        wist.Dispose();

        Assert.Throws<ObjectDisposedException>(() => wist.Compile<Func<double>>("1 + 2"));
    }

    [Test]
    public void CompiledDelegate_RemainsUsableAfterOriginatingEngineIsDisposed()
    {
        var wist = WistEngine.CreateRestrictedArithmetic();
        var program = wist.Compile<Func<double, double, double>>(
            "price * 0.9 + fee",
            "price",
            "fee");
        wist.Dispose();

        Assert.That(program.CompiledDelegate(100.0d, 5.0d), Is.EqualTo(95.0d).Within(1e-9));
    }
}
