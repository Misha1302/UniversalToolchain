using AbstractIrExtensions;
using AssemblyFinder;
using BasicCore.Binding;
using BasicCore.Binding.Symbols;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;

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

    private void HandleBoundVariable(BytecodeVisitorData data, Symbol symbol)
    {
        var varName = symbol.Name;
        if (symbol.Type != typeof(object) || !_variablesTypes.ContainsKey(varName))
            _variablesTypes[varName] = symbol.Type;

        if (data.Node.AllTags.Contains("ExpectingSettableReference"))
        {
            var method = new AbstractMethodImpl(
                $"LoadReferenceToVar_{varName}",
                (il, context) =>
                {
                    var type = symbol.Type != typeof(object) ? symbol.Type : context.Stack.Last();
                    _variablesTypes[varName] = type;
                    il.LdLocRef(varName, type);
                }
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
            return;
        }

        var loadName = symbol is ExternalVariableSymbol or ExternalConstantSymbol ? "LoadValueOfExternalVar" : "LoadValueOfLocalVar";
        var loadMethod = new AbstractMethodImpl(
            $"{loadName}_{varName}",
            (il, _) => il.LdLoc(varName, _variablesTypes[varName])
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(loadMethod));
    }

    private void HandlePreprocessorLexeme(BytecodeVisitorData data)
    {
        var text = data.Node.Text[3..^1].Split();
        if (text is not ["define", _, "as", _]) return;

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
        var varName = data.Node.Text;

        if (data.Node.AllTags.Contains("ExpectingSettableReference"))
        {
            var method = new AbstractMethodImpl(
                $"LoadReferenceToLocalVar_{varName}",
                (il, context) =>
                {
                    var type = _variablesTypes[varName] = context.Stack.Last();
                    il.LdLocRef(varName, type);
                }
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
        else
        {
            var method = new AbstractMethodImpl(
                $"LoadValueOfLocalVar_{varName}",
                (il, _) => il.LdLoc(varName, _variablesTypes[varName])
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
    }
}
