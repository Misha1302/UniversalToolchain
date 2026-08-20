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

        var diagnostics = ContractNamespacePolicy.ValidateOwnership(id, ContractNamespaceOwner.Reserved("wist", "wist"));

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.InvalidNamespaceOwnership));
    }


    [Test]
    public void ValidateOwnership_WithPackageDefinedReservation_DoesNotRequireGenericAssemblyChange()
    {
        var owner = ContractNamespaceOwner.Reserved("vendor-optimizer", "vendor.optimizer");
        var id = CreateId("vendor.optimizer.rules", "fold");

        var diagnostics = ContractNamespacePolicy.ValidateOwnership(id, owner, [owner]);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ValidateOwnership_ExternalOwnerCannotUsePackageDefinedReservation()
    {
        var reservation = ContractNamespaceOwner.Reserved("vendor", "vendor.contracts");
        var id = CreateId("vendor.contracts.rules", "fold");

        var diagnostics = ContractNamespacePolicy.ValidateOwnership(
            id,
            ContractNamespaceOwner.External("extension"),
            [reservation]);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(ModuleContractDiagnosticCodes.InvalidNamespaceOwnership));
    }

    [Test]
    public void ValidateOwnership_WhenIdentifierEqualsReservedRoot_AcceptsRoot()
    {
        var diagnostics = ContractNamespacePolicy.ValidateOwnership(
            "wist",
            ContractNamespaceOwner.Reserved("wist", "wist"));

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void Build_WithDeclaredNamespaceOwner_ValidatesOwnedFacetIdentifiers()
    {
        var module = new ModuleId("wist.example");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new BackendCapabilityFacet(
                module,
                [new BackendCapabilityContract(new BackendCapabilityId("core.backend.invalid-owner"), [])]))
            .AddNamespaceOwners(module, [ContractNamespaceOwner.Reserved("wist", "wist")])
            .Build();

        Assert.That(
            table.Diagnostics.Select(static diagnostic => diagnostic.Code),
            Does.Contain(ModuleContractDiagnosticCodes.InvalidNamespaceOwnership));
    }

    [Test]
    public void Build_WithMultipleDeclaredNamespaces_AcceptsBackendModuleAndCapabilityPrefixes()
    {
        var module = new ModuleId("backend.example");
        var packageNamespace = ContractNamespaceOwner.Reserved("example-backend", "example");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new BackendCapabilityFacet(
                module,
                [new BackendCapabilityContract(new BackendCapabilityId("example.backend.native"), [])]))
            .AddNamespaceOwners(module, [ContractNamespaceOwner.Backend, packageNamespace])
            .Build();

        Assert.That(
            table.Diagnostics.Where(static diagnostic =>
                diagnostic.Code == ModuleContractDiagnosticCodes.InvalidNamespaceOwnership),
            Is.Empty);
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
