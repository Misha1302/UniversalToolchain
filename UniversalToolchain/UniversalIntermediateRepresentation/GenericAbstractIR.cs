namespace UniversalIntermediateRepresentation;

// ReSharper disable once InconsistentNaming
public class GenericAbstractIR<TIdentifier>
{
    private readonly List<Instruction> _instructions = [];
    public IReadOnlyList<Instruction> Instructions => _instructions;

    public void Nop()
    {
        _instructions.Add(new Instruction(OpCode.Nop));
    }

    public void Push(Value value)
    {
        _instructions.Add(new Instruction(OpCode.Push, [value]));
    }

    public void Drop()
    {
        _instructions.Add(new Instruction(OpCode.Drop));
    }

    public void Jmp(TIdentifier identifier)
    {
        _instructions.Add(new Instruction(OpCode.Jmp, [Value.Create(identifier)]));
    }

    public void JmpIf(TIdentifier identifier)
    {
        _instructions.Add(new Instruction(OpCode.JmpIf, [Value.Create(identifier)]));
    }

    public void JmpIfNot(TIdentifier identifier)
    {
        _instructions.Add(new Instruction(OpCode.JmpIfNot, [Value.Create(identifier)]));
    }

    public void SetLabel(TIdentifier label)
    {
        _instructions.Add(new Instruction(OpCode.Label, [Value.Create(label)]));
    }

    public void Annotate(params List<Value>[] annotations)
    {
        _instructions.AddRange(annotations.Select(ann => new Instruction(OpCode.Annotate, ann)));
    }

    public void StLoc(TIdentifier index)
    {
        _instructions.Add(new Instruction(OpCode.StLoc, [Value.Create(index)]));
    }

    public void LdLoc(TIdentifier index)
    {
        _instructions.Add(new Instruction(OpCode.LdLoc, [Value.Create(index)]));
    }

    public void Intrinsic(Value instructionIdentifier, params List<Value> operands)
    {
        _instructions.Add(new Instruction(OpCode.Intrinsic, [instructionIdentifier, ..operands]));
    }
}