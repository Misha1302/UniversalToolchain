namespace UniversalToolchain.Dialects.Tests.Architecture;

public sealed class SsaAndLegacyDocumentationGuardrailTests
{
    [Test]
    public void SsaCoverageMatrix_DocumentsUnsupportedShapeDiagnostics()
    {
        var matrix = File.ReadAllText(FindRepositoryFile("docs", "architecture", "ssa-coverage-matrix.md"));

        Assert.Multiple(() =>
        {
            Assert.That(matrix, Does.Contain("air.to-ssa.stack-underflow"));
            Assert.That(matrix, Does.Contain("air.to-ssa.return-arity"));
            Assert.That(matrix, Does.Contain("air.to-ssa.return-type"));
            Assert.That(matrix, Does.Contain("air.to-ssa.push-type"));
            Assert.That(matrix, Does.Contain("air.to-ssa.opcode"));
            Assert.That(matrix, Does.Contain("Differential tests per intrinsic family"));
        });
    }

    [Test]
    public void LegacyCompatibilityBurnDown_DocumentsKnownProductionLegacyPaths()
    {
        var debt = File.ReadAllText(FindRepositoryFile("docs", "technical-debt.md"));
        var knownPaths = new[]
        {
            "`InstructionIntrinsicReader` legacy decoder fallback",
            "LegacyAirOptimizerStage",
            "ModuleContractEnforcementPolicy.LegacyCompatible",
            "Wist facade compatibility APIs"
        };

        Assert.Multiple(() =>
        {
            foreach (var path in knownPaths)
                Assert.That(debt, Does.Contain(path));

            Assert.That(debt, Does.Contain("New production legacy fallback requires an owner, replacement and removal gate"));
        });
    }

    private static string FindRepositoryFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate repository file.", Path.Combine(relativeParts));
    }
}
