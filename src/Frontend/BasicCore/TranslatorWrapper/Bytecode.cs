namespace BasicCore.TranslatorWrapper;

public record Bytecode(List<BytecodeInstruction> Instructions)
{
    public override string ToString()
    {
        return string.Join("\n", Instructions.Select(x => x.ToString()));
    }
}