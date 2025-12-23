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

    public void Jmp()
    {
        // on stack should be:
        // 1. Where to jump
        _instructions.Add(new Instruction(OpCode.Jmp));
    }

    public void JmpIf()
    {
        // on stack should be:
        // 1. Where to jump
        // 2. Boolean - jump or not
        _instructions.Add(new Instruction(OpCode.JmpIf));
    }

    public void JmpIfNot()
    {
        // on stack should be:
        // 1. Where to jump
        // 2. Boolean - jump or not
        _instructions.Add(new Instruction(OpCode.JmpIfNot));
    }

    public void SetLabel(TIdentifier label)
    {
        _instructions.Add(new Instruction(OpCode.Label, [Value.Create(label)]));
    }

    public void LoadIp()
    {
        _instructions.Add(new Instruction(OpCode.LoadIp));
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