namespace UniversalToolchain.ModuleContracts;

public sealed record StackEffect(int PopCount, int PushCount)
{
    public static StackEffect Unknown { get; } = new(-1, -1);

    public bool IsKnown => PopCount >= 0 && PushCount >= 0;
}
