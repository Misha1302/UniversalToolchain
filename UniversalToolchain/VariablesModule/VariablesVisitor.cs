using ExceptionsManager;

namespace VariablesModule;

public class VariablesVisitor : IAstVisitor
{
    private readonly OrderedDictionary<string, Type> _variablesTypes = [];

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Preprocessor lexeme"))
        {
            HandlePreprocessorLexeme(data);
            return;
        }

        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable"))
            return;

        if (data.Node is BoundAstNode bound)
            HandleBoundVariable(data, bound.Symbol);
        else
            HandleVariable(data);
    }

    private static bool IsConcreteType(Type type)
    {
        return type != typeof(object);
    }

    private Type ResolveReadType(Symbol symbol, string variableKey)
    {
        if (_variablesTypes.TryGetValue(variableKey, out var existing))
        {
            if (IsConcreteType(existing))
                return existing;

            if (symbol is ExternalVariableSymbol or ExternalConstantSymbol)
                return existing;
        }

        if (IsConcreteType(symbol.Type))
        {
            _variablesTypes[variableKey] = symbol.Type;
            return symbol.Type;
        }

        Thrower.InvalidOpEx(
            $"Storage type for variable '{symbol.Name}' is not fixed before read. " +
            $"Current symbol type: '{symbol.Type.FullName}'.");
        return null!;
    }

    private Type ResolveWriteType(Symbol symbol, string variableKey, Type inferredType)
    {
        if (_variablesTypes.TryGetValue(variableKey, out var existing) && IsConcreteType(existing))
            return existing;

        if (IsConcreteType(symbol.Type))
        {
            _variablesTypes[variableKey] = symbol.Type;
            return symbol.Type;
        }

        if (IsConcreteType(inferredType))
        {
            _variablesTypes[variableKey] = inferredType;
            return inferredType;
        }

        _variablesTypes[variableKey] = typeof(object);
        return typeof(object);
    }

    private void HandleBoundVariable(BytecodeVisitorData data, Symbol symbol)
    {
        var variableKey = symbol.StorageKey;
        var displayName = symbol.Name;
        if (IsConcreteType(symbol.Type))
            _variablesTypes[variableKey] = symbol.Type;
        else if (symbol is ExternalVariableSymbol or ExternalConstantSymbol && !_variablesTypes.ContainsKey(variableKey))
            _variablesTypes[variableKey] = symbol.Type;

        if (data.Node.AllTags.Contains("ExpectingSettableReference"))
        {
            var method = new AbstractMethodImpl(
                $"LoadReferenceToVar_{displayName}",
                (il, context) =>
                {
                    var inferredType = context.Stack.Last();
                    var storageType = ResolveWriteType(symbol, variableKey, inferredType);
                    il.LdLocRef(variableKey, storageType);
                }
            );

            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
            return;
        }

        var loadName = symbol is ExternalVariableSymbol or ExternalConstantSymbol
            ? "LoadValueOfExternalVar"
            : "LoadValueOfLocalVar";

        var loadMethod = new AbstractMethodImpl(
            $"{loadName}_{displayName}",
            (il, _) =>
            {
                var storageType = ResolveReadType(symbol, variableKey);
                il.LdLoc(variableKey, storageType);
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(loadMethod));
    }

    private void HandlePreprocessorLexeme(BytecodeVisitorData data)
    {
        var text = data.Node.Text[3..^1].Split();
        if (text is not ["define", _, "as", _])
            return;

        var paramName = text[1];
        var type = TypesFinder.GetType(text[3]);
        _variablesTypes[paramName] = type;
        var method = new AbstractMethodImpl(
            $"DefineArgument_{paramName}_{type.FullName}",
            (_, _) => _variablesTypes[paramName] = type
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void HandleVariable(BytecodeVisitorData data)
    {
        var variableKey = data.Node.Text;

        if (data.Node.AllTags.Contains("ExpectingSettableReference"))
        {
            var method = new AbstractMethodImpl(
                $"LoadReferenceToLocalVar_{data.Node.Text}",
                (il, context) =>
                {
                    var inferredType = context.Stack.Last();

                    var storageType =
                        _variablesTypes.TryGetValue(variableKey, out var existing) && IsConcreteType(existing)
                            ? existing
                            : IsConcreteType(inferredType)
                                ? (_variablesTypes[variableKey] = inferredType)
                                : typeof(object);

                    il.LdLocRef(variableKey, storageType);
                }
            );

            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
            return;
        }

        var loadMethod = new AbstractMethodImpl(
            $"LoadValueOfLocalVar_{data.Node.Text}",
            (il, _) =>
            {
                var storageType = _variablesTypes.TryGetValue(variableKey, out var existingType)
                    ? existingType
                    : Thrower.InvalidOpEx<Type>($"Storage type for variable '{data.Node.Text}' is not fixed before read.");

                il.LdLoc(variableKey, storageType);
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(loadMethod));
    }
}
