using NumbersModule.Contracts;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class BytecodeContractMetadataTests
{
    [Test]
    public void Verify_WhenInstructionHasContractPatternMetadata_DoesNotUseLegacyOperationName()
    {
        var bytecode = new Bytecode(
        [
            new BytecodeInstruction(new AbstractMethodImpl("legacy-runtime-name", (_, _) => { }))
                .WithContract(
                    NumbersContractIds.Module,
                    NumbersContractIds.NumberNode,
                    NumbersContractIds.PushRealNumber)
        ]);
        var table = new ModuleContractTableBuilder()
            .AddFacets(new NumbersModuleContractDescriptorProvider().GetFacets())
            .Build();

        var result = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
            bytecode,
            table,
            VerificationSeverityProfile.Strict,
            VerifyLegacyOperationNames: false));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Read_WhenInstructionHasContractMetadata_ProducesObservedEmissionForDriftChecks()
    {
        var bytecode = new Bytecode(
        [
            new BytecodeInstruction(new AbstractMethodImpl("PushNumber_1", (_, _) => { }))
                .WithContract(
                    NumbersContractIds.Module,
                    NumbersContractIds.NumberNode,
                    NumbersContractIds.PushRealNumber)
        ]);

        var observed = new BytecodeObservedEmissionReader().Read(bytecode);

        Assert.That(observed, Has.Count.EqualTo(1));
        Assert.That(observed[0].ProducerModule, Is.EqualTo(NumbersContractIds.Module));
        Assert.That(observed[0].SourceNode, Is.EqualTo(NumbersContractIds.NumberNode));
        Assert.That(observed[0].Patterns, Is.EqualTo(new[] { NumbersContractIds.PushRealNumber }));
    }

    [Test]
    public void NumbersModuleImpl_ShouldExposeRuntimeSelectableDescriptorProvider()
    {
        var module = new NumbersModule.Module.NumbersModuleImpl();

        Assert.That(module, Is.InstanceOf<IModuleContractDescriptorProvider>());
        Assert.That(
            ((IModuleContractDescriptorProvider)module).GetFacets().Select(static x => x.ModuleId),
            Does.Contain(NumbersContractIds.Module));
    }

    [Test]
    public void ReadWithDiagnostics_WhenInstructionHasConflictingProducerMetadata_ReturnsDiagnostic()
    {
        var instruction = new BytecodeInstruction(new AbstractMethodImpl("PushNumber_1", (_, _) => { }))
            .WithContract(
                NumbersContractIds.Module,
                NumbersContractIds.NumberNode,
                NumbersContractIds.PushRealNumber);
        instruction.Tags.Add(BytecodeContractMetadata.ProducerModule(new ModuleId("wist.other")));
        var bytecode = new Bytecode([instruction]);

        var result = new BytecodeObservedEmissionReader().ReadWithDiagnostics(bytecode);

        Assert.That(
            result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.InvalidBytecodeContractMetadata));
    }

    [Test]
    public void Observer_WhenVerifierReportsWarning_ReportsDiagnosticsToSink()
    {
        var sink = new RecordingSink();
        var observer = CreateObserver(
            ModuleContractPipelineProfiles.MigrationWarn,
            sink);
        var bytecode = new Bytecode(
        [
            new BytecodeInstruction(new AbstractMethodImpl("legacy-runtime-name", (_, _) => { }))
                .WithContract(
                    NumbersContractIds.Module,
                    NumbersContractIds.NumberNode,
                    new BytecodePatternId("wist.numbers.bytecode.not-declared"))
        ]);

        observer.AfterBytecode(new CompilationPipelineBytecodeContext(
            new CompilationInput { SourceText = "1" },
            [new NumbersModule.Module.NumbersModuleImpl()],
            bytecode));

        Assert.That(
            sink.Batches.SelectMany(static x => x.Diagnostics).Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.UnknownBytecodePattern));
    }

    [Test]
    public void Observer_WhenStrictBytecodeProfileReportsError_ThrowsAfterReportingDiagnostics()
    {
        var sink = new RecordingSink();
        var observer = CreateObserver(
            ModuleContractPipelineProfiles.StrictEnforced with
            {
                BytecodeProfile = VerificationSeverityProfile.Strict
            },
            sink);
        var bytecode = new Bytecode(
        [
            new BytecodeInstruction(new AbstractMethodImpl("legacy-runtime-name", (_, _) => { }))
                .WithContract(
                    NumbersContractIds.Module,
                    NumbersContractIds.NumberNode,
                    new BytecodePatternId("wist.numbers.bytecode.not-declared"))
        ]);

        Assert.Throws<InvalidOperationException>(() => observer.AfterBytecode(new CompilationPipelineBytecodeContext(
            new CompilationInput { SourceText = "1" },
            [new NumbersModule.Module.NumbersModuleImpl()],
            bytecode)));
        Assert.That(
            sink.Batches.SelectMany(static x => x.Diagnostics).Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.UnknownBytecodePattern));
    }

    private static ModuleContractPipelineObserver CreateObserver(
        ModuleContractPipelineOptions options,
        IModuleContractDiagnosticSink sink) =>
        new(
            options,
            new SelectedModuleContractTableProvider(
                options.EnforcementPolicy,
                new ModuleContractSelectionBuilder()),
            new BytecodeObservedEmissionReader(),
            new BytecodeVerifier(),
            new AirVerifier(),
            new BackendCapabilitySelectionFactory(options.BackendPolicy),
            new ModuleContractDiagnosticPolicy(sink),
            new PipelineEffectVerifier(),
            CompilerFactVerifierRegistry.Core,
            new CoreCompilerStageFactSeedProvider());

    private sealed class RecordingSink : IModuleContractDiagnosticSink
    {
        public List<ModuleContractPipelineDiagnosticBatch> Batches { get; } = [];

        public void Report(ModuleContractPipelineDiagnosticBatch batch) => Batches.Add(batch);
    }
}
