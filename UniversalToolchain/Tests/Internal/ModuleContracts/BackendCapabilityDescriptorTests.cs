using BasicCilCompiler.Contracts;
using BasicInterpreter.Contracts;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class BackendCapabilityDescriptorTests
{
    [Test]
    public void RuntimeSelectedContractTable_ShouldIncludeBackendDescriptorsFromSelectedBackendComponents()
    {
        var provider = new SelectedModuleContractTableProvider(
            ModuleContractPipelineProfiles.StrictEnforced.EnforcementPolicy,
            new ModuleContractSelectionBuilder());

        var report = provider.Build(
            [],
            [],
            [
                new ModuleContractBackendPipelineComponent(
                    CilBackendContractDescriptorProvider.Module.Value,
                    [new CilBackendContractDescriptorProvider(["load_i32", "cmp_eq_i32"])])
            ]);

        Assert.That(report, Is.Not.Null);
        Assert.That(report!.Diagnostics, Is.Empty);

        var selectedCapabilityIds = report.ContractTable.BackendCapabilityFacets
            .SelectMany(static facet => facet.Capabilities)
            .Select(static capability => capability.CapabilityId)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(selectedCapabilityIds, Does.Contain(KnownCilBackendCapabilities.DynamicMethods));
            Assert.That(selectedCapabilityIds, Does.Not.Contain(KnownInterpreterBackendCapabilities.UniversalIntrinsicsOnly));
        });
    }


    [Test]
    public void AuxiliaryBackendPipelineComponents_ShouldNotBecomeSelectedContractModules()
    {
        var provider = new SelectedModuleContractTableProvider(
            ModuleContractPipelineProfiles.StrictEnforced.EnforcementPolicy,
            new ModuleContractSelectionBuilder());

        var report = provider.Build(
            [],
            [],
            [new AuxiliaryBackendComponent("runtime-provider-policy")]);

        Assert.That(report, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(report!.Diagnostics, Is.Empty);
            Assert.That(
                report.ModuleStatuses.Select(static status => status.ModuleId.Value),
                Does.Not.Contain("backend.runtime.provider.policy"));
        });
    }

    [Test]
    public void BackendSpecificDescriptorProviders_ShouldUseBackendOwnedCapabilityNamespaces()
    {
        var cilCapabilityIds = GetCapabilityIds(new CilBackendContractDescriptorProvider());
        var interpreterCapabilityIds = GetCapabilityIds(new InterpreterBackendContractDescriptorProvider());

        Assert.Multiple(() =>
        {
            Assert.That(cilCapabilityIds, Is.Not.Empty);
            Assert.That(interpreterCapabilityIds, Is.Not.Empty);
            Assert.That(cilCapabilityIds.All(static id => id.StartsWith("cil.backend.", StringComparison.Ordinal)), Is.True);
            Assert.That(interpreterCapabilityIds.All(static id => id.StartsWith("interpreter.backend.", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void KnownCoreBackendCapabilities_ShouldStayBackendNeutral()
    {
        var coreCapabilityIds = KnownCoreBackendCapabilities.CreateFacet()
            .Capabilities
            .Select(static capability => capability.CapabilityId.Value)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(coreCapabilityIds.Any(static id => id.StartsWith("cil.", StringComparison.Ordinal)), Is.False);
            Assert.That(coreCapabilityIds.Any(static id => id.StartsWith("interpreter.", StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public void CilIntrinsicCapabilityContracts_ShouldDeclareConcreteSupportedIntrinsicSurface()
    {
        var intrinsicCapabilities = GetCapabilities(new CilBackendContractDescriptorProvider(
                ["load_i32", "add_i32", "cmp_eq_i32", "cmp_lt_i32"]))
            .Where(static capability =>
                capability.CapabilityId == KnownCilBackendCapabilities.NativeNumericIntrinsics ||
                capability.CapabilityId == KnownCilBackendCapabilities.NativeComparisonIntrinsics)
            .ToArray();

        var placeholderCapabilities = intrinsicCapabilities
            .Where(static capability => capability.SupportedIntrinsics.Count == 0)
            .Select(static capability => capability.CapabilityId.Value)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(intrinsicCapabilities, Is.Not.Empty);
            Assert.That(
                placeholderCapabilities,
                Is.Empty,
                "Intrinsic backend capabilities must describe a concrete intrinsic surface, not just a capability name.");
        });
    }

    [Test]
    public void InterpreterDescriptor_ShouldNotExposeCilCapabilityIds()
    {
        var interpreterCapabilityIds = GetCapabilities(new InterpreterBackendContractDescriptorProvider(["call C#", "call C# ctor"]))
            .Select(static capability => capability.CapabilityId.Value)
            .ToArray();

        Assert.That(interpreterCapabilityIds.Any(static id => id.StartsWith("cil.", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void BackendCapabilitySelectionFactory_ShouldUseContractDeclaredIntrinsicSurfaceWhenAvailable()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacets(new CilBackendContractDescriptorProvider(["load_i32"]).GetFacets())
            .Build();

        var selection = new BackendCapabilitySelectionFactory(AirBackendPolicy.CapabilityGated)
            .Create(table, ["load_i32", "not_declared_by_backend_contract"]);

        Assert.That(
            selection.SupportedIntrinsics.Select(static intrinsic => intrinsic.Value),
            Is.EqualTo(new[] { "load_i32" }));
    }

    [Test]
    public void BackendCapabilitySelectionFactory_ShouldRejectAccidentalMultiBackendCapabilityTables()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacets(new CilBackendContractDescriptorProvider(["load_i32"]).GetFacets())
            .AddFacets(new InterpreterBackendContractDescriptorProvider(["call C#"]).GetFacets())
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            new BackendCapabilitySelectionFactory(AirBackendPolicy.CapabilityGated)
                .Create(table, ["load_i32", "call C#"]));
    }

    private sealed class AuxiliaryBackendComponent(string componentId) : IBackendPipelineComponent
    {
        public string ComponentId { get; } = componentId;
    }

    private static IReadOnlyList<string> GetCapabilityIds(IModuleContractDescriptorProvider provider) =>
        GetCapabilities(provider)
            .Select(static capability => capability.CapabilityId.Value)
            .ToArray();

    private static IReadOnlyList<BackendCapabilityContract> GetCapabilities(IModuleContractDescriptorProvider provider) =>
        provider.GetFacets()
            .OfType<IBackendCapabilityFacet>()
            .Single()
            .Capabilities;
}
