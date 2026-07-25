using System.Text.Json;
using UniversalToolchain.PlanFuzz.Adapter.Acme;
using UniversalToolchain.PlanFuzz.Cli;

namespace UniversalToolchain.PlanFuzz.IntegrationTests;

[TestFixture]
public sealed class StrictReplayTests
{
    private string _temporaryDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "planfuzz-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Test]
    public async Task CleanCaseReplaysInFreshProcessesWithoutFindings()
    {
        var casePath = await GenerateCaseAsync(faultId: null);
        var output = Path.Combine(_temporaryDirectory, "clean-replay");
        var exitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "replay", "--case", casePath, "--output", output,
            "--repeat", "2", "--timeout-seconds", "20"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(PlanFuzzExitCodes.Success));
            Assert.That(File.Exists(Path.Combine(output, "replay-report.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(output, "MANIFEST.sha256")), Is.True);
            Assert.That(Directory.GetDirectories(Path.Combine(output, "attempts")), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public async Task ReplayRejectsNonEmptyOutputDirectory()
    {
        var casePath = await GenerateCaseAsync(faultId: null);
        var output = Path.Combine(_temporaryDirectory, "stale-replay");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "stale.txt"), "stale evidence");

        var exitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "replay", "--case", casePath, "--output", output,
            "--repeat", "1", "--timeout-seconds", "20"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(PlanFuzzExitCodes.InvalidCase));
            Assert.That(File.Exists(Path.Combine(output, "stale.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(output, "replay-report.json")), Is.False);
        });
    }

    [Test]
    public async Task SeededFaultIsConfirmedThreeOutOfThreeWithStableFingerprint()
    {
        var casePath = await GenerateCaseAsync(AcmePlanFuzzConstants.WrongArithmeticFault);
        var output = Path.Combine(_temporaryDirectory, "fault-replay");
        var exitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "replay", "--case", casePath, "--output", output,
            "--repeat", "3", "--timeout-seconds", "20"
        ]);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "replay-report.json")));
        var root = document.RootElement;
        var fingerprints = root.GetProperty("attempts").EnumerateArray()
            .Select(static item => item.GetProperty("fingerprint").GetString())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(PlanFuzzExitCodes.Finding));
            Assert.That(root.GetProperty("confirmedViolation").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("inconclusive").GetBoolean(), Is.False);
            Assert.That(fingerprints.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(File.ReadAllLines(Path.Combine(output, "MANIFEST.sha256")), Has.Length.GreaterThan(10));
        });
    }

    [TestCase(AcmePlanFuzzConstants.ExcludedActivationFault)]
    [TestCase(AcmePlanFuzzConstants.ExtensionInterferenceFault)]
    public async Task SurfaceSeededFaultIsConfirmedInFreshProcesses(string faultId)
    {
        var casePath = await GenerateCaseAsync(faultId);
        var output = Path.Combine(_temporaryDirectory, $"surface-fault-{faultId}");
        var exitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "replay", "--case", casePath, "--output", output,
            "--repeat", "2", "--timeout-seconds", "20"
        ]);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "replay-report.json")));
        var attempts = document.RootElement.GetProperty("attempts").EnumerateArray().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(PlanFuzzExitCodes.Finding));
            Assert.That(document.RootElement.GetProperty("confirmedViolation").GetBoolean(), Is.True);
            Assert.That(attempts, Has.Length.EqualTo(2));
            Assert.That(attempts.Select(static attempt => attempt.GetProperty("fingerprint").GetString())
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ReplayWithoutOracleContractsReturnsInconclusive()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var generated = adapter.GenerateCase(123, 0, new PlanFuzzCaseGenerationOptions());
        var incomplete = new PlanFuzzTestCase(
            generated.SchemaVersion,
            generated.AdapterId,
            generated.AdapterVersion,
            generated.CampaignSeed,
            generated.CaseIndex,
            generated.CaseSeed,
            generated.PrngAlgorithm,
            generated.Program,
            generated.Variants,
            []);
        var casePath = Path.Combine(_temporaryDirectory, "incomplete-case.json");
        await File.WriteAllTextAsync(casePath, PlanFuzzTestCaseSerializer.Serialize(incomplete));
        var output = Path.Combine(_temporaryDirectory, "incomplete-replay");

        var exitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "replay", "--case", casePath, "--output", output,
            "--repeat", "2", "--timeout-seconds", "20"
        ]);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "replay-report.json")));

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(PlanFuzzExitCodes.Inconclusive));
            Assert.That(document.RootElement.GetProperty("inconclusive").GetBoolean(), Is.True);
            Assert.That(document.RootElement.GetProperty("flaky").GetBoolean(), Is.False);
            Assert.That(document.RootElement.GetProperty("confirmedViolation").GetBoolean(), Is.False);
        });
    }

    [Test]
    public async Task AcmeRejectsTheWistOnlyRegressionCorpusOption()
    {
        var casePath = Path.Combine(_temporaryDirectory, "unsupported-regressions.json");

        var exitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "generate", "--adapter", AcmePlanFuzzConstants.AdapterId,
            "--seed", "123", "--index", "0", "--out", casePath,
            "--include-regressions"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(PlanFuzzExitCodes.InvalidCase));
            Assert.That(File.Exists(casePath), Is.False);
        });
    }

    private async Task<string> GenerateCaseAsync(string? faultId)
    {
        var casePath = Path.Combine(_temporaryDirectory, faultId == null ? "clean-case.json" : "fault-case.json");
        var args = new List<string>
        {
            "generate", "--adapter", AcmePlanFuzzConstants.AdapterId,
            "--seed", "123", "--index", "0", "--out", casePath
        };
        if (faultId != null)
        {
            args.Add("--fault");
            args.Add(faultId);
        }
        var exitCode = await PlanFuzzCommandHost.RunAsync(args.ToArray());
        Assert.That(exitCode, Is.EqualTo(PlanFuzzExitCodes.Success));
        return casePath;
    }
}
