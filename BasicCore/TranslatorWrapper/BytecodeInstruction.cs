// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using DynamicMethodWrapper;

namespace BasicCore.TranslatorWrapper;

public record BytecodeInstruction(HashSet<string> Tags, LevelCollection<float, IDynamicMethodConvertable> Ops)
{
    public BytecodeInstruction(IDynamicMethodConvertable op)
        : this([], new LevelCollection<float, IDynamicMethodConvertable> { { 0, op } })
    {
    }

    public override string ToString()
    {
        return
            $"[{string.Join(", ", Tags.Select(x => x.ToString()))}] [{string.Join(", ", Ops.Select(x => $"{x.Key}={string.Join(", ", x.Value)}"))}]";
    }
}