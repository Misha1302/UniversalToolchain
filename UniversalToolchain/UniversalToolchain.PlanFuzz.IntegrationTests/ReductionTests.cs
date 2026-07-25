using System.Text.Json;
using UniversalToolchain.PlanFuzz.Adapter.Acme;
using UniversalToolchain.PlanFuzz.Cli;

namespace UniversalToolchain.PlanFuzz.IntegrationTests;

[TestFixture]
public sealed class ReductionTests
{
    private string _temporaryDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "planfuzz-reduction-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Test]
    public async Task SeededFaultReductionPreservesExactFingerprintAndShrinksThePlan()
    {
        var casePath = Path.Combine(_temporaryDirectory, "fault-case.json");
        var generateExitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "generate", "--adapter", AcmePlanFuzzConstants.AdapterId,
            "--seed", "123", "--index", "0", "--out", casePath,
            "--fault", AcmePlanFuzzConstants.WrongArithmeticFault
        ]);
        Assert.That(generateExitCode, Is.EqualTo(PlanFuzzExitCodes.Success));
        var original = PlanFuzzTestCaseSerializer.Deserialize(await File.ReadAllTextAsync(casePath));
        var output = Path.Combine(_temporaryDirectory, "reduction");

        var reduceExitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "reduce", "--case", casePath, "--output", output,
            "--repeat", "2", "--timeout-seconds", "20", "--max-candidates", "50"
        ]);

        var reportText = await File.ReadAllTextAsync(Path.Combine(output, "reduction-report.json"));
        var reducedText = await File.ReadAllTextAsync(Path.Combine(output, "reduced-case.json"));
        using var reportDocument = JsonDocument.Parse(reportText);
        var report = reportDocument.RootElement;
        var reduced = PlanFuzzTestCaseSerializer.Deserialize(reducedText);
        Assert.Multiple(() =>
        {
            Assert.That(reduceExitCode, Is.EqualTo(PlanFuzzExitCodes.Finding));
            Assert.That(report.GetProperty("completed").GetBoolean(), Is.True);
            Assert.That(report.GetProperty("acceptedSteps").GetInt32(), Is.GreaterThan(0));
            Assert.That(report.GetProperty("targetFingerprint").GetString(),
                Is.EqualTo(report.GetProperty("finalFingerprint").GetString()));
            Assert.That(reduced.Variants.Count, Is.LessThan(original.Variants.Count));
            Assert.That(reduced.OracleContracts.Count, Is.LessThan(original.OracleContracts.Count));
            Assert.That(File.Exists(Path.Combine(output, "MANIFEST.sha256")), Is.True);
            Assert.That(Directory.GetDirectories(Path.Combine(output, "candidates")), Is.Not.Empty);
        });
    }

    [Test]
    public async Task ReductionRejectsACleanCaseButStillWritesAuditableEvidence()
    {
        var casePath = Path.Combine(_temporaryDirectory, "clean-case.json");
        var generateExitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "generate", "--adapter", AcmePlanFuzzConstants.AdapterId,
            "--seed", "123", "--index", "0", "--out", casePath
        ]);
        Assert.That(generateExitCode, Is.EqualTo(PlanFuzzExitCodes.Success));
        var output = Path.Combine(_temporaryDirectory, "clean-reduction");

        var reduceExitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "reduce", "--case", casePath, "--output", output,
            "--repeat", "1", "--timeout-seconds", "20", "--max-candidates", "10"
        ]);
        using var reportDocument = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(output, "reduction-report.json")));

        Assert.Multiple(() =>
        {
            Assert.That(reduceExitCode, Is.EqualTo(PlanFuzzExitCodes.InvalidCase));
            Assert.That(reportDocument.RootElement.GetProperty("completed").GetBoolean(), Is.False);
            Assert.That(reportDocument.RootElement.GetProperty("candidateEvaluations").GetInt32(), Is.Zero);
            Assert.That(File.Exists(Path.Combine(output, "reduced-case.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(output, "MANIFEST.sha256")), Is.True);
        });
    }
}
