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
    public void CompatibilityBoundary_DocumentsOnlyExplicitUndeclaredModuleObservation()
    {
        var debt = File.ReadAllText(FindRepositoryFile("internal-docs", "policies-and-reports", "technical-debt.md"));

        Assert.Multiple(() =>
        {
            Assert.That(debt, Does.Contain("ModuleContractEnforcementPolicy.AllowUndeclared"));
            Assert.That(debt, Does.Not.Contain("LegacyAirOptimizerStage"));
            Assert.That(debt, Does.Not.Contain("legacy decoder fallback"));
            Assert.That(debt, Does.Not.Contain("CompileFunc"));
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
