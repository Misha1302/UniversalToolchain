namespace BasicCore.Binding.Symbols;

public abstract class Symbol(string name, Type type)
{
    public string Name { get; } = name;
    public Type Type { get; } = type;
}