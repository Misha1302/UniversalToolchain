using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistUseCaseRecipeTests
{
    private const string DocumentedRolloutFormula =
        "usage * 0.7 + reliability * 0.3 - incidents * 15.0";
    private const double DocumentedRolloutExpectedScore = 82.0;

    [Test]
    public void RolloutScoreRecipe_UsesDocumentedFormulaThroughPublicFacade()
    {
        using var rules = WistEngine.CreateRestrictedArithmetic();

        var validation = rules.Validate(
            DocumentedRolloutFormula,
            new
            {
                usage = 100.0,
                reliability = 90.0,
                incidents = 1.0
            });

        var rolloutScore = rules.Compile<Func<double, double, double, double>>(
            DocumentedRolloutFormula,
            "usage",
            "reliability",
            "incidents");
        var score = rolloutScore.CompiledDelegate(100.0, 90.0, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.True);
            Assert.That(validation.Diagnostics, Is.Empty);
            Assert.That(score, Is.EqualTo(DocumentedRolloutExpectedScore).Within(1e-9));
        });
    }
}
