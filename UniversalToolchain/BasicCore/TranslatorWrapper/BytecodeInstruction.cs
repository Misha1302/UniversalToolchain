namespace BasicCore.TranslatorWrapper;

public record BytecodeInstruction(HashSet<string> Tags, LevelCollection<float, IAbstractMethodConvertable> Ops)
{
    public BytecodeInstruction(IAbstractMethodConvertable op)
        : this([], new LevelCollection<float, IAbstractMethodConvertable> { { 0, op } })
    {
    }

    public override string ToString()
    {
        return
            $"[{string.Join(", ", Tags.Select(x => x.ToString()))}] [{string.Join(", ", Ops.Select(x => $"{x.Key}={string.Join(", ", x.Value)}"))}]";
    }
}