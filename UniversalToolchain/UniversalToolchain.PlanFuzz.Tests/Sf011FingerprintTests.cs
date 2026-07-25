using UniversalToolchain.PlanFuzz.Adapter.Acme;

namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class Sf011FingerprintTests
{
    [Test]
    public void ExtensionInterferenceExactFingerprintIncludesTheCompleteViolationMechanism()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var testCase = adapter.GenerateCase(
            123,
            0,
            new PlanFuzzCaseGenerationOptions(AcmePlanFuzzConstants.ExtensionInterferenceFault));
        var observations = testCase.Variants.Select(variant => adapter.Execute(testCase, variant)).ToArray();
        var results = new PlanFuzzOracleEngine().Evaluate(testCase, observations);
        var attempt = new PlanFuzzReplayAttempt(1, observations, results);
        var violation = results.Single(result =>
            StringComparer.Ordinal.Equals(result.ContractId, "extension-noninterference.seeded"));

        TestContext.Progress.WriteLine($"SF-011 exact fingerprint: {attempt.Fingerprint}");
        TestContext.Progress.WriteLine($"SF-011 class fingerprint: {attempt.ClassFingerprint}");

        Assert.Multiple(() =>
        {
            Assert.That(violation.Status, Is.EqualTo(PlanFuzzOracleStatus.Violated));
            Assert.That(violation.FingerprintMaterial, Does.Contain("extension-activated:"));
            Assert.That(violation.FingerprintMaterial, Does.Contain("behavior:"));
            Assert.That(violation.EffectiveClassFingerprintMaterial, Does.Contain("extension-activated"));
            Assert.That(violation.EffectiveClassFingerprintMaterial, Does.Contain("behavior:"));
            Assert.That(attempt.Fingerprint, Is.Not.EqualTo(attempt.ClassFingerprint));
        });
    }
}
