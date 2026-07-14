using BasicCore.Core;
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
        _instructions.Add(new Instruction(UOpCode.Push, [AirPushOperand.Create(value)]));
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
        annotations = annotations.ArgNotNull();
        _instructions.AddRange(annotations.Select(ann => new Instruction(UOpCode.Annotate, ann)));
    }

    public void Intrinsic(string capabilityId, params object?[] dataOperands)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("Intrinsic capability identifier must not be empty.", nameof(capabilityId));

        dataOperands = dataOperands.ArgNotNull();
        _instructions.Add(IntrinsicInstructionFactory.CreateForCapability(capabilityId, dataOperands));
    }


    public void AppendInstructions(IReadOnlyList<Instruction> instructions)
    {
        instructions = instructions.ArgNotNull();
        _instructions.AddRange(instructions);
    }
}