namespace BasicInterpreter;

public class InterpreterState
{
    private readonly Dictionary<Guid, int> _labelPositions = new();
    private readonly Dictionary<string, object?> _locals = new(StringComparer.Ordinal);
    private bool _labelsBuilt;
    public Stack<object> ValueStack { get; } = new();

    /// <summary>
    /// Compile-time binding layout used to resolve declared external symbols at runtime.
    /// Symbol class (local vs external) must be determined before interpreter execution.
    /// Runtime state is not allowed to infer or redefine symbol class from observed calls.
    /// </summary>
    public ExternalBindingsLayout? ExternalBindingsLayout { get; set; }

    public int InstructionPointer { get; set; }
    public IExecutionEnvironment? ExecutionEnvironment { get; set; }

    public void BuildLabelPositions(IReadOnlyList<Instruction> instructions)
    {
        if (_labelsBuilt)
            return;

        _labelPositions.Clear();
        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].UOpCode == UOpCode.Label)
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
            Thrower.InvalidOpEx($"Label with id '{labelId}' was not found in the instruction stream.");

        return position;
    }

    public object GetLocalValue(string name, Type runtimeType)
    {
        if (_locals.TryGetValue(name, out var value) && value != null)
            return value;

        var defaultValue = runtimeType.IsValueType
            ? Activator.CreateInstance(runtimeType)
            : null;

        _locals[name] = defaultValue;

        if (defaultValue == null)
            Thrower.InvalidOpEx($"Local '{name}' is null and cannot be loaded.");

        return defaultValue;
    }

    public void SetLocalValue(string name, object? value)
    {
        _locals[name] = value;
    }
}
