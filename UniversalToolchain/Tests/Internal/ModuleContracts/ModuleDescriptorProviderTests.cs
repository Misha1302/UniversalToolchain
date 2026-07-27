using LabelsModule.Contracts;
using NumbersModule.Contracts;
using IdentifierModule.Contracts;
using ScopesModule.Contracts;
using UniversalToolchain.ModuleContracts;
using VariablesModule.Contracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ModuleDescriptorProviderTests
{
    [Test]
    public void Build_WhenRepresentativeModulesAreSelected_ProducesDeterministicDeclaredTable()
    {
        var report = BuildRepresentativeReport();

        var orderedFacets = report.ContractTable.Facets
            .Select(static x => $"{x.ModuleId.Value}:{x.Kind}")
            .ToArray();

        Assert.That(report.Diagnostics, Is.Empty);
        Assert.That(
            orderedFacets,
            Is.EqualTo(new[]
            {
                "core.backend-capabilities:BackendCapability",
                "core.compiler-facts:CompilerFacts",
                "wist.identifiers:Syntax",
                "wist.identifiers:CompilerFacts",
                "wist.identifiers:PipelineEffects",
                "wist.labels:Syntax",
                "wist.labels:Ast",
                "wist.labels:Bytecode",
                "wist.labels:Air",
                "wist.labels:CompilerFacts",
                "wist.labels:PipelineEffects",
                "wist.numbers:Syntax",
                "wist.numbers:Ast",
                "wist.numbers:Bytecode",
                "wist.numbers:Air",
                "wist.numbers:CompilerFacts",
                "wist.numbers:PipelineEffects",
                "wist.scopes:Syntax",
                "wist.scopes:CompilerFacts",
                "wist.scopes:PipelineEffects",
                "wist.variables:Syntax",
                "wist.variables:Ast",
                "wist.variables:Bytecode",
                "wist.variables:Air",
                "wist.variables:CompilerFacts",
                "wist.variables:PipelineEffects"
            }));
        Assert.That(
            report.ModuleStatuses.Select(static x => x.Status),
            Is.All.EqualTo(ModuleContractCompatibilityStatus.Declared));
    }

    [Test]
    public void Build_WhenSelectedModuleHasNoDescriptor_ClassifiesAsUndeclaredWarning()
    {
        var report = new ModuleContractSelectionBuilder().Build(
            [
                NumbersContractIds.Module,
                new ModuleId("wist.legacy")
            ],
            [
                new NumbersModuleContractDescriptorProvider()
            ],
            ModuleContractEnforcementPolicy.AllowUndeclared);

        Assert.That(report.Diagnostics.Select(static x => x.Code), Does.Contain(ModuleContractDiagnosticCodes.UndeclaredModule));
        Assert.That(
            report.ModuleStatuses.Single(static x => x.ModuleId == new ModuleId("wist.legacy")).Status,
            Is.EqualTo(ModuleContractCompatibilityStatus.Undeclared));
    }

    [Test]
    public void DescriptorProviders_ExposeClaimsTraceableToCurrentRepresentativeModules()
    {
        var numbers = new NumbersModuleContractDescriptorProvider().GetFacets();
        var variables = new VariablesModuleContractDescriptorProvider().GetFacets();
        var labels = new LabelsModuleContractDescriptorProvider().GetFacets();
        var identifiers = new IdentifierModuleContractDescriptorProvider().GetFacets();
        var scopes = new ScopesModuleContractDescriptorProvider().GetFacets();

        Assert.That(numbers.OfType<ISyntaxContractFacet>().Single().Lexemes.Select(static x => x.LexemeId), Does.Contain("Number"));
        Assert.That(variables.OfType<ISyntaxContractFacet>().Single().Lexemes.Select(static x => x.LexemeId), Does.Contain("Let"));
        Assert.That(labels.OfType<ISyntaxContractFacet>().Single().Lexemes.Select(static x => x.LexemeId), Does.Contain("Goto"));
        Assert.That(identifiers.OfType<ICompilerFactOwnershipFacet>().Single().Facts.Select(static x => x.FactId), Does.Contain(IdentifierFacts.IdentifiersAvailable));
        Assert.That(scopes.OfType<ICompilerFactOwnershipFacet>().Single().Facts.Select(static x => x.FactId), Does.Contain(ScopesFacts.ScopesLocalsBound));
    }

    [Test]
    public void DescriptorProviders_ExposePipelineEffectsForRepresentativeModules()
    {
        var report = BuildRepresentativeReport();

        var numbersEffect = FindEffect(report.ContractTable, NumbersContractIds.Module, NumbersEffects.LowerNumericLiteral);
        var variablesEffect = FindEffect(report.ContractTable, VariablesContractIds.Module, VariablesEffects.LowerVariableAccess);
        var labelsEffect = FindEffect(report.ContractTable, LabelsContractIds.Module, LabelsEffects.LowerLabelControlFlow);

        Assert.That(numbersEffect.Produces, Does.Contain(NumbersFacts.NumericValuesSupported));
        Assert.That(
            report.ContractTable.AirFacets
                .Single(static x => x.ModuleId == NumbersContractIds.Module)
                .AirEmissions
                .SelectMany(static x => x.RequiredCapabilities),
            Does.Contain(KnownCoreBackendCapabilities.ObjectConstruction));
        Assert.That(variablesEffect.Requires, Does.Contain(ScopesFacts.ScopesLocalsBound));
        Assert.That(
            report.ContractTable.AirFacets
                .Single(static x => x.ModuleId == VariablesContractIds.Module)
                .AirEmissions
                .SelectMany(static x => x.RequiredCapabilities),
            Does.Contain(KnownCoreBackendCapabilities.LocalVariables));
        Assert.That(labelsEffect.Produces, Does.Contain(LabelsFacts.GotosResolved));
        Assert.That(
            labelsEffect.Invalidates,
            Does.Contain(KnownCoreCompilerFacts.AirVerified));
        Assert.That(
            labelsEffect.Invalidates,
            Does.Not.Contain(KnownCoreCompilerFacts.BytecodeVerified));
    }

    [Test]
    public void RuntimeTableProvider_WhenStrictProfileSeesSelectedModuleWithoutDescriptor_ReturnsError()
    {
        var provider = new SelectedModuleContractTableProvider(
            ModuleContractPipelineProfiles.StrictEnforced.EnforcementPolicy,
            new ModuleContractSelectionBuilder());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.Build([new UndeclaredFrontendModule()], []));

        Assert.That(exception!.Message, Does.Contain("must declare DialectRuntimeExportAttribute"));
    }

    private sealed class UndeclaredFrontendModule : IFrontendCoreModule;

    private static ModuleContractSelectionReport BuildRepresentativeReport() =>
        new ModuleContractSelectionBuilder().Build(
            [
                KnownCoreModuleIds.CompilerFacts,
                KnownCoreModuleIds.BackendCapabilities,
                IdentifierContractIds.Module,
                VariablesContractIds.Module,
                LabelsContractIds.Module,
                NumbersContractIds.Module,
                ScopesContractIds.Module
            ],
            [
                new KnownCoreContractDescriptorProvider(),
                new IdentifierModuleContractDescriptorProvider(),
                new VariablesModuleContractDescriptorProvider(),
                new LabelsModuleContractDescriptorProvider(),
                new NumbersModuleContractDescriptorProvider(),
                new ScopesModuleContractDescriptorProvider()
            ],
            ModuleContractPipelineProfiles.StrictEnforced.EnforcementPolicy);

    private static PipelineEffectContract FindEffect(
        SelectedModuleContractTable table,
        ModuleId moduleId,
        CompilerEffectId effectId) =>
        table.PipelineEffectFacets
            .Single(x => x.ModuleId == moduleId)
            .Effects
            .Single(x => x.EffectId == effectId);
}
