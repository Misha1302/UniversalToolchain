using UniversalIntermediateRepresentation;

namespace BasicInterpreter;

public class InterpreterState
{
    private readonly Dictionary<Guid, int> _labelPositions = new();
    private bool _labelsBuilt;
    public Stack<Value> ValueStack { get; } = new();
    public Dictionary<Guid, Value> Locals { get; } = new();

    public int InstructionPointer { get; set; }

    public void BuildLabelPositions(IReadOnlyList<Instruction> instructions)
    {
        if (_labelsBuilt) return;

        _labelPositions.Clear();
        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode == OpCode.Label)
            {
                var labelId = instructions[i].Operands[0].Get<Guid>();
                _labelPositions[labelId] = i;
            }
        }

        _labelsBuilt = true;
    }

    public int GetLabelPosition(Guid labelId)
    {
        if (!_labelPositions.TryGetValue(labelId, out var position))
            throw new InvalidOperationException($"Label {labelId} not found");

        return position;
    }
}