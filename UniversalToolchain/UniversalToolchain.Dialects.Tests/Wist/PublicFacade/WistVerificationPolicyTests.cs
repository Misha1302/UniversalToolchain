using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistVerificationPolicyTests
{
    [TestCase(WistVerificationPolicy.P0Structural)]
    [TestCase(WistVerificationPolicy.P1Invalidation)]
    [TestCase(WistVerificationPolicy.P2Selective)]
    [TestCase(WistVerificationPolicy.P3Always)]
    public void Create_WithExplicitPolicy_EvaluatesValidProgram(WistVerificationPolicy policy)
    {
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
            VerificationPolicy = policy
        });

        Assert.That(engine.Evaluate<double>("1 + 2"), Is.EqualTo(3.0d).Within(1e-9));
    }

    [Test]
    public void Create_RejectsUnknownPolicy()
    {
        var options = new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
            VerificationPolicy = (WistVerificationPolicy)int.MaxValue
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => WistEngine.Create(options));
    }

    [Test]
    public void Create_SnapshotsVerificationPolicy()
    {
        var options = new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
            VerificationPolicy = WistVerificationPolicy.P3Always
        };
        using var engine = WistEngine.Create(options);

        options.VerificationPolicy = (WistVerificationPolicy)int.MaxValue;

        Assert.That(engine.Evaluate<double>("2 + 3"), Is.EqualTo(5.0d).Within(1e-9));
    }
}
