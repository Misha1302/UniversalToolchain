using AbstractIrExtensions;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;

namespace VariablesModule;

public class VariablesVisitor : IAstVisitor
{
    private readonly Dictionary<string, Type> _variableTypes = [];

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable"))
            return;

        var varName = data.Node.Text;

        if (data.Node.AllTags.Contains("ExpectingSettableReference"))
        {
            var method = new AbstractMethodImpl(
                $"LoadReferenceToLocalVar_{varName}",
                (il, context) =>
                {
                    var type = _variableTypes[varName] = context.Stack[0];
                    il.LdLocRef(varName, type);
                }
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
        else
        {
            var method = new AbstractMethodImpl(
                $"LoadValueOfLocalVar_{varName}",
                (il, _) => il.LdLoc(varName, _variableTypes[varName])
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
    }
}