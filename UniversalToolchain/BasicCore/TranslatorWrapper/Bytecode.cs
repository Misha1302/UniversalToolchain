// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace BasicCore.TranslatorWrapper;

public record Bytecode(List<BytecodeInstruction> Instructions)
{
    public override string ToString()
    {
        return string.Join("\n", Instructions.Select(x => x.ToString()));
    }
}