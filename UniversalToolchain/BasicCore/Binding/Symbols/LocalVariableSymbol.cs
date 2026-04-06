namespace BasicCore.Binding.Symbols;

public sealed class LocalVariableSymbol : Symbol
{
    private readonly string _storageKey;

    public LocalVariableSymbol(string name, Type type)
        : base(name, type)
    {
        _storageKey = $"local:{Guid.NewGuid():N}:{name}";
    }

    public override string StorageKey => _storageKey;
}
