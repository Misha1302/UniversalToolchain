namespace BasicCore.Binding.Symbols;

public sealed class FunctionSymbol(string name, Type returnType) : Symbol(name, returnType);