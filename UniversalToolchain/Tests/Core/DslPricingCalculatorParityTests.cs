using Tests.Infrastructure;
using UniversalToolchain.Testing.Infrastructure;

namespace Tests.Core;

[TestFixture]
public class DslPricingCalculatorParityTests
{
    [Test]
    public void CanonicalRuntime_ParameterExecution_MatchesCilAndInterpreter()
    {
        using var host = new CanonicalWistTestHost();
        const string formula = "price * 0.9 + fee";
        var arguments = new Dictionary<string, object?>
        {
            ["price"] = 100.0,
            ["fee"] = 5.0
        };

        var compiler = host.Run(formula, "cil", arguments);
        var interpreter = host.Run(formula, "interpreter", arguments);

        Assert.Multiple(() =>
        {
            Assert.That(BackendParityInfrastructure.AsNumber(compiler), Is.EqualTo(95.0).Within(1e-9));
            Assert.That(BackendParityInfrastructure.AsNumber(interpreter), Is.EqualTo(95.0).Within(1e-9));
            Assert.That(
                BackendParityInfrastructure.AsNumber(compiler),
                Is.EqualTo(BackendParityInfrastructure.AsNumber(interpreter)).Within(1e-9));
        });
    }
}
