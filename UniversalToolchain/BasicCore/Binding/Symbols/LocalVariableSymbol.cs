namespace BasicCore.Binding.Symbols;

public sealed class LocalVariableSymbol : Symbol
{
    public LocalVariableSymbol(string name, Type type)
        : base(name, type)
    {
        StorageKey = $"local:{Guid.NewGuid():N}:{name}";
    }

    public override string StorageKey { get; }
}