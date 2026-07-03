using BasicCilCompiler.Contracts;
using BasicInterpreter.Contracts;
using IdentifierModule.Contracts;
using LabelsModule.Contracts;
using NumbersModule.Contracts;
using ScopesModule.Contracts;
using UniversalToolchain.ModuleContracts;
using VariablesModule.Contracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ContractGraphConsistencyTests
{
    [Test]
    public void RepresentativeWistContractGraph_ShouldResolveEveryPipelineFactReference()
    {
        var report = BuildRepresentativeReport();
        var table = report.ContractTable;
        var factOwnerships = ReadFactOwnerships(table);
        var duplicateOwners = factOwnerships
            .GroupBy(static ownership => ownership.FactId)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key.Value)
            .ToArray();
        var factsByOwner = factOwnerships
            .GroupBy(static ownership => ownership.FactId)
            .ToDictionary(static group => group.Key, static group => group.First().OwnerModule);

        var referencedFacts = table.PipelineEffectFacets
            .SelectMany(static facet => facet.Effects)
            .SelectMany(static effect => effect.Requires
                .Concat(effect.Produces)
                .Concat(effect.Preserves)
                .Concat(effect.Invalidates))
            .Distinct()
            .OrderBy(static fact => fact.Value, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(table.Diagnostics, Is.Empty);
            Assert.That(report.Diagnostics, Is.Empty);
            Assert.That(duplicateOwners, Is.Empty);
            Assert.That(
                referencedFacts.Where(fact => !factsByOwner.ContainsKey(fact)).Select(static fact => fact.Value),
                Is.Empty,
                "Every fact referenced by pipeline effects must be declared by exactly one ownership facet.");
        });
    }

    [Test]
    public void RepresentativeWistContractGraph_ShouldOnlyProduceFactsOwnedByProducerModule()
    {
        var table = BuildRepresentativeReport().ContractTable;
        var factsByOwner = ReadFactOwnerships(table)
            .GroupBy(static ownership => ownership.FactId)
            .ToDictionary(static group => group.Key, static group => group.First().OwnerModule);

        var foreignProductions = table.PipelineEffectFacets
            .SelectMany(facet => facet.Effects.SelectMany(effect => effect.Produces.Select(fact => (facet.ModuleId, effect.EffectId, Fact: fact))))
            .Where(item => factsByOwner.TryGetValue(item.Fact, out var owner) && owner != item.ModuleId)
            .Select(static item => $"{item.ModuleId.Value}:{item.EffectId.Value}->{item.Fact.Value}")
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.That(foreignProductions, Is.Empty);
    }

    [Test]
    public void RepresentativeWistContractGraph_ShouldRouteInvalidatedCoreBoundaryFactsToVerifierRules()
    {
        var table = BuildRepresentativeReport().ContractTable;
        var registry = CompilerFactVerifierRegistry.Core;
        var unroutedCoreInvalidations = table.PipelineEffectFacets
            .SelectMany(static facet => facet.Effects)
            .SelectMany(static effect => effect.Invalidates)
            .Where(static fact => fact.Value.StartsWith("core.", StringComparison.Ordinal))
            .Distinct()
            .Where(fact => !registry.TryGetVerifier(fact, out _))
            .Select(static fact => fact.Value)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.That(unroutedCoreInvalidations, Is.Empty);
    }

    private static ModuleContractSelectionReport BuildRepresentativeReport() =>
        new ModuleContractSelectionBuilder().Build(
            [
                KnownCoreModuleIds.CompilerFacts,
                KnownCoreModuleIds.BackendCapabilities,
                IdentifierContractIds.Module,
                LabelsContractIds.Module,
                NumbersContractIds.Module,
                ScopesContractIds.Module,
                VariablesContractIds.Module,
                CilBackendContractDescriptorProvider.Module,
                InterpreterBackendContractDescriptorProvider.Module
            ],
            [
                new KnownCoreContractDescriptorProvider(),
                new IdentifierModuleContractDescriptorProvider(),
                new LabelsModuleContractDescriptorProvider(),
                new NumbersModuleContractDescriptorProvider(),
                new ScopesModuleContractDescriptorProvider(),
                new VariablesModuleContractDescriptorProvider(),
                new CilBackendContractDescriptorProvider(["load_i32", "cmp_eq_i32"]),
                new InterpreterBackendContractDescriptorProvider(["call C#", "call C# ctor"])
            ],
            ModuleContractPipelineProfiles.StrictEnforced.EnforcementPolicy);

    private static IReadOnlyList<(CompilerFactId FactId, ModuleId OwnerModule)> ReadFactOwnerships(SelectedModuleContractTable table) =>
        table.CompilerFactOwnershipFacets
            .SelectMany(static facet => facet.Facts.Select(fact => (fact.FactId, fact.OwnerModule)))
            .ToArray();
}
