using ExceptionsManager;

namespace BasicCore.Binding.Symbols;

public sealed class LocalVariableSymbol : Symbol
{
    internal LocalVariableSymbol(string name, Type type, int declarationOrdinal)
        : base(name, type)
    {
        if (declarationOrdinal < 0)
            Thrower.ArgumentOutOfRange<int>(nameof(declarationOrdinal), "Declaration ordinal must be non-negative.");

        StorageKey = $"local:{declarationOrdinal:D8}:{name}";
    }

    public override string StorageKey { get; }
}
