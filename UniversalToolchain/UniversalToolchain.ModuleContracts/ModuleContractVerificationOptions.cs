namespace UniversalToolchain.ModuleContracts;

public enum ModuleContractVerificationMode
{
    Strict,
    Warn,
    Off
}

/// <summary>Host-owned module-contract verification policy and observable diagnostic sink.</summary>
public sealed class ModuleContractVerificationOptions
{
    public ModuleContractVerificationMode Mode { get; init; } = ModuleContractVerificationMode.Strict;

    public ModuleContractPipelineOptions PipelineOptions { get; init; } =
        ModuleContractPipelineProfiles.StrictEnforced;

    public IModuleContractDiagnosticSink DiagnosticSink { get; init; } =
        new InMemoryModuleContractDiagnosticSink();

    public ModuleContractVerificationOptions SnapshotValidated()
    {
        var pipeline = PipelineOptions ?? throw new ArgumentNullException(nameof(PipelineOptions));
        if (!Enum.IsDefined(pipeline.VerificationPolicy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(PipelineOptions),
                pipeline.VerificationPolicy,
                "Unknown module-contract verification policy.");
        }
        var sink = DiagnosticSink ?? throw new ArgumentNullException(nameof(DiagnosticSink));

        if (Mode != ModuleContractVerificationMode.Off && sink is NullModuleContractDiagnosticSink)
        {
            throw new ArgumentException(
                "Enabled module-contract verification requires an observable diagnostic sink.",
                nameof(DiagnosticSink));
        }

        var effective = Mode switch
        {
            ModuleContractVerificationMode.Strict => pipeline with
            {
                Enabled = true,
                BytecodeProfile = VerificationSeverityProfile.Strict,
                AirProfile = VerificationSeverityProfile.Strict
            },
            ModuleContractVerificationMode.Warn => pipeline with
            {
                Enabled = true,
                BytecodeProfile = VerificationSeverityProfile.Warn,
                AirProfile = VerificationSeverityProfile.Warn
            },
            ModuleContractVerificationMode.Off => pipeline with { Enabled = false },
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unknown verification mode.")
        };

        return new ModuleContractVerificationOptions
        {
            Mode = Mode,
            PipelineOptions = effective,
            DiagnosticSink = sink
        };
    }

    public static ModuleContractVerificationOptions StrictDefault() => new();

    public static ModuleContractVerificationOptions Warn(IModuleContractDiagnosticSink sink) => new()
    {
        Mode = ModuleContractVerificationMode.Warn,
        PipelineOptions = ModuleContractPipelineProfiles.Warn,
        DiagnosticSink = sink
    };

    public static ModuleContractVerificationOptions Off() => new()
    {
        Mode = ModuleContractVerificationMode.Off,
        PipelineOptions = ModuleContractPipelineProfiles.Observe with { Enabled = false },
        DiagnosticSink = NullModuleContractDiagnosticSink.Instance
    };
}
