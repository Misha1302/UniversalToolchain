using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ModuleContractEnforcementPolicyTests
{
    [Test]
    public void Build_WhenNewModuleHasNoDescriptor_ReturnsErrorDiagnostic()
    {
        var moduleId = new ModuleId("test.new-module");
        var policy = ModuleContractEnforcementPolicy.EnforceNewModules([]);

        var report = new ModuleContractSelectionBuilder().Build([moduleId], [], policy);

        Assert.That(report.Diagnostics.Single().Code, Is.EqualTo(ModuleContractDiagnosticCodes.NewModuleMissingDescriptor));
        Assert.That(report.Diagnostics.Single().Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Error));
        Assert.That(report.ModuleStatuses.Single().Status, Is.EqualTo(ModuleContractCompatibilityStatus.LegacyImplicit));
    }

    [Test]
    public void Build_WhenLegacyModuleHasNoDescriptor_KeepsExplicitLegacyStatus()
    {
        var moduleId = new ModuleId("test.legacy-module");
        var policy = ModuleContractEnforcementPolicy.EnforceNewModules(
            [new ModuleContractStatusDeclaration(moduleId, ModuleContractCompatibilityStatus.LegacyImplicit)]);

        var report = new ModuleContractSelectionBuilder().Build([moduleId], [], policy);

        Assert.That(report.Diagnostics.Single().Code, Is.EqualTo(ModuleContractDiagnosticCodes.LegacyImplicitModule));
        Assert.That(report.Diagnostics.Single().Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Warning));
        Assert.That(report.ModuleStatuses.Single().Status, Is.EqualTo(ModuleContractCompatibilityStatus.LegacyImplicit));
    }

    [Test]
    public void Build_WhenDeclaredModuleHasNoDescriptor_ReturnsStatusMismatchError()
    {
        var moduleId = new ModuleId("test.declared-without-descriptor");
        var policy = ModuleContractEnforcementPolicy.EnforceNewModules(
            [new ModuleContractStatusDeclaration(moduleId, ModuleContractCompatibilityStatus.Declared)]);

        var report = new ModuleContractSelectionBuilder().Build([moduleId], [], policy);

        Assert.That(report.Diagnostics.Select(static x => x.Code), Does.Contain(ModuleContractDiagnosticCodes.DeclaredModuleMissingDescriptor));
        Assert.That(report.Diagnostics.Any(static x => x.Severity == ToolchainDiagnosticSeverity.Error), Is.True);
        Assert.That(report.ModuleStatuses.Single().Status, Is.EqualTo(ModuleContractCompatibilityStatus.Declared));
    }

    [Test]
    public void Build_WhenNewModuleHasDescriptor_AssignsDeclaredStatusWithoutDiagnostics()
    {
        var moduleId = new ModuleId("test.new-declared");
        var provider = new SingleFacetProvider(new AstContractFacet(
            moduleId,
            [
                new AstOwnershipContract(
                    new AstNodeKind("test.new-declared.node"),
                    AstOwnershipMode.Exclusive,
                    moduleId,
                    [])
            ]));
        var policy = ModuleContractEnforcementPolicy.EnforceNewModules([]);

        var report = new ModuleContractSelectionBuilder().Build([moduleId], [provider], policy);

        Assert.That(report.Diagnostics, Is.Empty);
        Assert.That(report.ModuleStatuses.Single().Status, Is.EqualTo(ModuleContractCompatibilityStatus.Declared));
    }

    [Test]
    public void SelectionBuilder_ShouldNotExposeImplicitLegacyCompatibleBuildOverload()
    {
        var implicitBuildOverloads = typeof(ModuleContractSelectionBuilder)
            .GetMethods()
            .Where(static method => method.Name == nameof(ModuleContractSelectionBuilder.Build))
            .Where(static method => method.GetParameters().Length == 2)
            .ToArray();

        Assert.That(
            implicitBuildOverloads,
            Is.Empty,
            "Production selection must pass an explicit enforcement policy; legacy compatibility is available only through BuildLegacyCompatible.");
    }

    private sealed class SingleFacetProvider(IModuleContractFacet facet) : IModuleContractDescriptorProvider
    {
        public IReadOnlyList<IModuleContractFacet> GetFacets() => [facet];
    }
}
