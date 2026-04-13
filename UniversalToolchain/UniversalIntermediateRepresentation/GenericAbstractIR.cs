using ExceptionsManager;

namespace UniversalIntermediateRepresentation;

// ReSharper disable once InconsistentNaming
public class GenericAbstractIR<TIdentifier> : IGenericAbstractIR<TIdentifier>
{
    private readonly List<Instruction> _instructions = [];
    public IReadOnlyList<Instruction> Instructions => _instructions;

    public void Nop()
    {
        _instructions.Add(new Instruction(UOpCode.Nop));
    }

    public void Push<T>(T value)
    {
        _instructions.Add(new Instruction(UOpCode.Push, [value!]));
    }

    public void Drop()
    {
        _instructions.Add(new Instruction(UOpCode.Drop));
    }

    public void Jmp(TIdentifier identifier)
    {
        _instructions.Add(new Instruction(UOpCode.Jmp, [identifier!]));
    }

    public void JmpIf(TIdentifier identifier)
    {
        _instructions.Add(new Instruction(UOpCode.JmpIf, [identifier!]));
    }

    public void JmpIfNot(TIdentifier identifier)
    {
        _instructions.Add(new Instruction(UOpCode.JmpIfNot, [identifier!]));
    }

    public void SetLabel(TIdentifier label)
    {
        _instructions.Add(new Instruction(UOpCode.Label, [label!]));
    }

    public void Annotate(params List<object>[] annotations)
    {
        if (annotations == null)
            Thrower.ArgumentNull(nameof(annotations));
        _instructions.AddRange(annotations.Select(ann => new Instruction(UOpCode.Annotate, ann)));
    }

    public void Intrinsic(object instructionIdentifier, params List<object> operands)
    {
        if (instructionIdentifier == null)
            Thrower.ArgumentNull(nameof(instructionIdentifier));
        if (operands == null)
            Thrower.ArgumentNull(nameof(operands));
        _instructions.Add(new Instruction(UOpCode.Intrinsic, [instructionIdentifier, ..operands]));
    }


    public void AppendInstructions(IReadOnlyList<Instruction> instructions)
    {
        if (instructions == null)
            Thrower.ArgumentNull(nameof(instructions));
        _instructions.AddRange(instructions);
    }
}
