namespace IntermediateRepresentationAbstractions;

// ReSharper disable once InconsistentNaming
public interface IGenericAbstractIR<TIdentifier>
{
    IReadOnlyList<Instruction> Instructions { get; }
    void Nop();
    void Push<T>(T value);
    void Drop();
    void Jmp(TIdentifier identifier);
    void JmpIf(TIdentifier identifier);
    void JmpIfNot(TIdentifier identifier);
    void SetLabel(TIdentifier label);
    void Annotate(params List<object>[] annotations);
    void Intrinsic(object instructionIdentifier, params List<object> operands);
    void AppendInstructions(IGenericAbstractIR<TIdentifier> air);
}