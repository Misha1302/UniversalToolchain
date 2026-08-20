namespace UniversalToolchain.Dialects.Tests.Architecture;

public sealed class SsaAndLegacyDocumentationGuardrailTests
{
    [Test]
    public void SsaCoverageMatrix_DocumentsUnsupportedShapeDiagnostics()
    {
        var matrixPath = FindRepositoryFileOrNull("docs", "architecture", "ssa-coverage-matrix.md");
        if (matrixPath is null)
        {
            Assert.Pass("Generic UniversalToolchain SSA documentation is intentionally absent from the Wist split repository.");
            return;
        }
        var matrix = File.ReadAllText(matrixPath);

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
        var debtPath = FindRepositoryFileOrNull("internal-docs", "policies-and-reports", "technical-debt.md");
        if (debtPath is null)
        {
            Assert.Pass("Generic UniversalToolchain technical-debt documentation is intentionally absent from the Wist split repository.");
            return;
        }
        var debt = File.ReadAllText(debtPath);

        Assert.Multiple(() =>
        {
            Assert.That(debt, Does.Contain("ModuleContractEnforcementPolicy.AllowUndeclared"));
            Assert.That(debt, Does.Not.Contain("LegacyAirOptimizerStage"));
            Assert.That(debt, Does.Not.Contain("legacy decoder fallback"));
            Assert.That(debt, Does.Not.Contain("CompileFunc"));
        });
    }

    private static string? FindRepositoryFileOrNull(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;

            // Do not walk out of a physical split repository to discover upstream source/docs.
            if (File.Exists(Path.Combine(directory.FullName, "eng", "component.json")))
                return null;

            directory = directory.Parent;
        }

        return null;
    }
}
