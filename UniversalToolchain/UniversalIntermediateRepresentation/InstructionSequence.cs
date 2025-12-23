using System.Collections;
using ExceptionsManager;

namespace UniversalIntermediateRepresentation;

public class InstructionSequence : IEnumerable<Instruction>
{
    private readonly List<Instruction> _instructions;
    private readonly Dictionary<string, int> _labelIndices;
    private bool _indicesDirty;

    public InstructionSequence(IEnumerable<Instruction>? instructions = null)
    {
        _instructions = new List<Instruction>(instructions ?? []);
        _labelIndices = new Dictionary<string, int>();
        RebuildLabelIndices();
    }

    public IReadOnlyList<Instruction> Instructions => _instructions;

    public IReadOnlyDictionary<string, int> LabelIndices
    {
        get
        {
            if (_indicesDirty) RebuildLabelIndices();
            return _labelIndices;
        }
    }

    public IEnumerator<Instruction> GetEnumerator()
    {
        return _instructions.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void RebuildLabelIndices()
    {
        _labelIndices.Clear();
        for (var i = 0; i < _instructions.Count; i++)
        {
            if (_instructions[i].OpCode != OpCode.Label)
                continue;

            var labelName = (string)_instructions[i].Operands[0].Data.NotNull();
            _labelIndices[labelName] = i;
        }
        _indicesDirty = false;
    }

    public void Add(Instruction instruction)
    {
        _instructions.Add(instruction);
        if (instruction.OpCode == OpCode.Label)
        {
            _indicesDirty = true;
        }
    }

    public void Insert(int index, Instruction instruction)
    {
        _instructions.Insert(index, instruction);
        _indicesDirty = true;
    }

    public void RemoveAt(int index)
    {
        _instructions.RemoveAt(index);
        _indicesDirty = true;
    }

    public void Clear()
    {
        _instructions.Clear();
        _labelIndices.Clear();
        _indicesDirty = false;
    }

    public InstructionSequence Clone()
    {
        return new InstructionSequence(_instructions);
    }

    public override string ToString()
    {
        return string.Join("\n", _instructions.Select((instr, i) => $"{i:D4}: {instr}"));
    }
}