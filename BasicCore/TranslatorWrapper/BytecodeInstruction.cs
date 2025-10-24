// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using DynamicMethodWrapper;

namespace BasicCore.TranslatorWrapper;

public record BytecodeInstruction(HashSet<string> Tags, SortedDictionary<float, IDynamicMethodConvertable> Ops)
{
    public override string ToString()
    {
        return
            $"[{string.Join(", ", Tags.Select(x => x.ToString()))}] [{string.Join(", ", Ops.Select(x => $"{x.Key}={x.Value.Name}"))}]";
    }
}