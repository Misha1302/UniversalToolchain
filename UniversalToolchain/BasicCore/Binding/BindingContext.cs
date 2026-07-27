namespace BasicCore.Binding;

public sealed class BindingContext
{
    private readonly Dictionary<string, Symbol> _externals;
    private readonly Dictionary<string, LocalVariableSymbol> _locals = new();
    private int _nextLocalDeclarationOrdinal;

    public BindingContext(IReadOnlyList<ExternalBinding> externalBindings)
    {
        externalBindings = externalBindings.ArgNotNull();

        _externals = externalBindings
            .Select((binding, slot) => CreateExternalSymbol(binding, slot))
            .ToDictionary(x => x.Name, x => x);
    }

    public LocalVariableSymbol DeclareLocal(string name, Type type)
    {
        var symbol = new LocalVariableSymbol(name, type, _nextLocalDeclarationOrdinal++);
        _locals[symbol.Name] = symbol;
        return symbol;
    }

    public bool TryGetLocal(string name, out LocalVariableSymbol symbol) => _locals.TryGetValue(name, out symbol!);

    public bool TryGetExternal(string name, out Symbol symbol) => _externals.TryGetValue(name, out symbol!);

    private static Symbol CreateExternalSymbol(ExternalBinding binding, int slot) => binding.Kind switch
    {
        ExternalBindingKind.Variable => new ExternalVariableSymbol(binding.Name, binding.Type, slot),
        ExternalBindingKind.Constant => new ExternalConstantSymbol(binding.Name, binding.Type, slot),
        _ => Thrower.InvalidOpEx<Symbol>($"Unsupported external binding kind: {binding.Kind}")
    };
}
