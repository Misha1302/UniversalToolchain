using AbstractIrExtensions;
using AssemblyFinder;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DotnetAirHelper;
using DynamicMethodWrapper;
using ListExtensions;
using ObjectExtensions;

namespace VariablesModule;

public class VariablesVisitor : IAstVisitor
{
    private readonly OrderedDictionary<string, Type> _variablesTypes = [];

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable"))
            HandleVariable(data);
        if (data.Node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Preprocessor lexeme"))
            HandlePreprocessorLexeme(data);
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