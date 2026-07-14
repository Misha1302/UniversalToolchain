namespace BasicInterpreter;

internal readonly record struct InterpreterStackValue(object? Value, Type DeclaredType);

public class InterpreterState
{
    private readonly Dictionary<Guid, int> _labelPositions = new();
    private readonly Stack<Type> _declaredTypes = new();
    private bool _labelsBuilt;

    /// <summary>
    ///     Preserves the original public interpreter-state surface. Interpreter-owned operations keep
    ///     declared AIR types in parallel metadata so typed null constants remain distinguishable.
    /// </summary>
    public Stack<object?> ValueStack { get; } = new();

    public int InstructionPointer { get; set; }
    public IExecutionEnvironment? ExecutionEnvironment { get; set; }

    internal int EvaluationStackCount => ValueStack.Count;

    internal void PushEvaluationValue(object? value, Type declaredType)
    {
        AlignDeclaredTypesWithPublicStack();
        ValueStack.Push(value);
        _declaredTypes.Push(declaredType.ArgNotNull());
    }

    internal InterpreterStackValue PopEvaluationValue()
    {
        AlignDeclaredTypesWithPublicStack();
        var value = ValueStack.Pop();
        var declaredType = _declaredTypes.Pop();
        return new InterpreterStackValue(value, declaredType);
    }

    internal InterpreterStackValue PeekEvaluationValue()
    {
        AlignDeclaredTypesWithPublicStack();
        return new InterpreterStackValue(ValueStack.Peek(), _declaredTypes.Peek());
    }

    internal void BuildLabelPositions(IReadOnlyList<Instruction> instructions)
    {
        if (_labelsBuilt)
            return;

        _labelPositions.Clear();
        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].UOpCode != UOpCode.Label)
                continue;

            var labelId = instructions[i].Operands.Count == 1 && instructions[i].Operands[0] is Guid id
                ? id
                : Thrower.InvalidOpEx<Guid>(
                    $"AIR label instruction at index {i} requires exactly one Guid operand.");

            _labelPositions[labelId] = i;
        }

        _labelsBuilt = true;
    }

    internal int GetLabelPosition(Guid labelId)
    {
        if (!_labelPositions.TryGetValue(labelId, out var position))
            Thrower.InvalidOpEx($"Label with id '{labelId}' was not found in the instruction stream.");

        return position;
    }

    private void AlignDeclaredTypesWithPublicStack()
    {
        if (_declaredTypes.Count == ValueStack.Count)
            return;

        // Public callers could mutate ValueStack directly because this was part of the existing API.
        // Reconstruct best-effort metadata for those externally supplied values. Interpreter-owned
        // pushes remain fully typed and do not enter this fallback path.
        _declaredTypes.Clear();
        foreach (var value in ValueStack.Reverse())
            _declaredTypes.Push(value?.GetType() ?? typeof(object));
    }
}
