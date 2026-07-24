using UniversalToolchain.PlanFuzz.Adapter.Wist;
using UniversalToolchain.PlanFuzz.Cli;

namespace UniversalToolchain.PlanFuzz.IntegrationTests;

[TestFixture]
public sealed class WistStrictReplayTests
{
    private string _temporaryDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "planfuzz-wist-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Test]
    public async Task CleanWistParameterCaseReplaysTwiceInFreshProcesses()
    {
        var adapter = new WistPlanFuzzAdapter();
        var testCase = adapter.CreateCase(
            123,
            100,
            100,
            new WistIntProgramModel(
                WistIntExpression.Add(WistIntExpression.Parameter(), WistIntExpression.Constant(3)),
                39,
                "integration-test"));
        var casePath = Path.Combine(_temporaryDirectory, "case.json");
        await File.WriteAllTextAsync(casePath, PlanFuzzTestCaseSerializer.Serialize(testCase));
        var output = Path.Combine(_temporaryDirectory, "replay");

        var exitCode = await PlanFuzzCommandHost.RunAsync(
        [
            "replay", "--case", casePath, "--output", output,
            "--repeat", "2", "--timeout-seconds", "60"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(PlanFuzzExitCodes.Success));
            Assert.That(File.Exists(Path.Combine(output, "replay-report.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(output, "MANIFEST.sha256")), Is.True);
            Assert.That(Directory.GetDirectories(Path.Combine(output, "attempts")), Has.Length.EqualTo(2));
        });
    }
}
