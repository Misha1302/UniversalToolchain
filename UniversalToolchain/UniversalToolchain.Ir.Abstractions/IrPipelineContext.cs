namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Carries immutable per-run legality state through IR stages.
/// </summary>
public sealed class IrPipelineContext
{
    public IrPipelineContext(CapabilitySet? capabilities = null, IrFactSet? facts = null)
    {
        Capabilities = capabilities ?? CapabilitySet.Empty;
        Facts = facts ?? IrFactSet.Empty;
    }

    public CapabilitySet Capabilities { get; }

    public IrFactSet Facts { get; }
}
