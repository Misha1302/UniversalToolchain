namespace UniversalToolchain.ModuleContracts;

public sealed class InternalVerifierException : InvalidOperationException
{
    public InternalVerifierException(
        string verifier,
        string phase,
        int instructionIndex,
        string blockId,
        Exception innerException)
        : base(
            $"Internal verifier failure in '{verifier}' during '{phase}' at AIR instruction {instructionIndex} " +
            $"in block '{blockId}'.",
            innerException)
    {
        Verifier = verifier;
        Phase = phase;
        InstructionIndex = instructionIndex;
        BlockId = blockId;
    }

    public string Verifier { get; }
    public string Phase { get; }
    public int InstructionIndex { get; }
    public string BlockId { get; }
}

internal sealed class AirVerificationDomainException : InvalidOperationException
{
    public AirVerificationDomainException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
