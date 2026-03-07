namespace BasicCore.Binding.Symbols;

public sealed class ExternalVariableSymbol(string name, Type type, int slot) : Symbol(name, type)
{
    public int Slot { get; } = slot;
}
