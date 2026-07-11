using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ContractNamespaceAndAliasTests
{
    [Test]
    public void ValidateUniqueIds_WhenIdsRepeatInsideNamespace_ReportsDuplicateDiagnostic()
    {
        var ids = new[]
        {
            CreateId("core.variable", "read"),
            CreateId("core.variable", "read"),
            CreateId("core.variable", "write")
        };

        var diagnostics = ContractIdRegistryValidator.ValidateUniqueIds(ids);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.DuplicateId));
    }

    [Test]
    public void ValidateOwnership_WhenWistOwnedIdUsesCoreNamespace_ReportsDiagnostic()
    {
        var id = CreateId("core.labels", "label");

        var diagnostics = ContractNamespacePolicy.ValidateOwnership(id, ContractNamespaceOwner.Wist);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.InvalidNamespaceOwnership));
    }

    [Test]
    public void Resolve_WhenLegacyAliasExists_ReturnsReplacementAndMigrationDiagnostic()
    {
        var replacement = CreateId("wist.variables", "write-target-type-inference");
        var catalog = new ContractAliasCatalog(
        [
            new CompatibilityAliasRecord(
                "ExpectingWriteTypeInference",
                replacement,
                ModuleContractSchemaVersions.Current,
                ModuleContractSchemaVersions.Current)
        ]);

        var result = catalog.Resolve("ExpectingWriteTypeInference");

        Assert.That(result.IsMatch, Is.True);
        Assert.That(result.Replacement, Is.EqualTo(replacement));
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.DeprecatedAlias));
    }

    [Test]
    public void Build_WhenFacetUsesNewerSchema_ReportsSchemaDowngradeDiagnostic()
    {
        var moduleId = new ModuleId("core.new-schema");
        var table = new ModuleContractTableBuilder
            {
                SupportedSchemaVersion = new ContractSchemaVersion(1, 0)
            }
            .AddFacet(new AstContractFacet(moduleId, [])
            {
                SchemaVersion = new ContractSchemaVersion(2, 0)
            })
            .Build();

        Assert.That(table.SchemaVersion, Is.EqualTo(new ContractSchemaVersion(1, 0)));
        Assert.That(table.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(table.Diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.SchemaDowngrade));
    }

    private static ContractId CreateId(string @namespace, string name) =>
        new(@namespace, name, ModuleContractSchemaVersions.Current);
}
