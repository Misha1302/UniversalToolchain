namespace BytecodeDynamicMethodsCompiler.Compilers;

internal sealed class CilAbstractIrTypeSimulator
{
    private readonly AbstractMethodsIntrinsicCompiler _intrinsicCompiler;

    public CilAbstractIrTypeSimulator()
        : this(new AbstractMethodsIntrinsicCompiler())
    {
    }

    internal CilAbstractIrTypeSimulator(AbstractMethodsIntrinsicCompiler intrinsicCompiler)
    {
        _intrinsicCompiler = intrinsicCompiler;
    }

    public List<Type> Simulate(IReadOnlyList<Instruction> instructions) => Simulate(instructions, new Dictionary<Guid, List<Type>>());

    public List<Type> Simulate(IReadOnlyList<Instruction> instructions, Dictionary<Guid, List<Type>> labelStacks)
    {
        var stack = new List<Type>();

        foreach (var instruction in instructions)
            ApplyInstruction(instruction, stack, labelStacks);

        return stack;
    }

    public void ApplyInstruction(Instruction instruction, List<Type> stack)
    {
        ApplyInstruction(instruction, stack, new Dictionary<Guid, List<Type>>());
    }

    public void ApplyInstruction(Instruction instruction, List<Type> stack, Dictionary<Guid, List<Type>> labelStacks)
    {
        ApplyInstruction(instruction, stack, (IDictionary<Guid, List<Type>>)labelStacks);
    }

    private void ApplyInstruction(
        Instruction instruction,
        List<Type> stack,
        IDictionary<Guid, List<Type>> labelStacks
    )
    {
        switch (instruction.UOpCode)
        {
            case UOpCode.Nop:
            case UOpCode.Annotate:
                return;
            case UOpCode.Push:
                stack.Push(instruction.Operands[0].GetType());
                return;
            case UOpCode.Drop:
                stack.Pop();
                return;
            case UOpCode.Jmp:
                SaveLabelStack(instruction, stack, labelStacks);
                return;
            case UOpCode.JmpIf:
            case UOpCode.JmpIfNot:
                Thrower.AssertAlways(stack.Count >= 1, $"Not enough values on stack for {instruction.UOpCode}");
                stack.Pop();
                SaveLabelStack(instruction, stack, labelStacks);
                return;
            case UOpCode.Label:
                RestoreLabelStack(instruction, stack, labelStacks);
                return;
            case UOpCode.Intrinsic:
                _intrinsicCompiler.ProcessTypes(instruction, stack);
                return;
            default:
                Thrower.InvalidOpEx($"Unknown opcode {instruction.UOpCode}");
                return;
        }
    }

    private static void SaveLabelStack(
        Instruction instruction,
        IReadOnlyCollection<Type> stack,
        IDictionary<Guid, List<Type>> labelStacks
    )
    {
        var labelId = instruction.Operands[0].Get<Guid>();
        labelStacks[labelId] = [.. stack];
    }

    private static void RestoreLabelStack(
        Instruction instruction,
        List<Type> stack,
        IDictionary<Guid, List<Type>> labelStacks
    )
    {
        var labelId = instruction.Operands[0].Get<Guid>();
        if (!labelStacks.TryGetValue(labelId, out var savedStack))
            return;

        stack.Clear();
        stack.AddRange(savedStack);
    }
}