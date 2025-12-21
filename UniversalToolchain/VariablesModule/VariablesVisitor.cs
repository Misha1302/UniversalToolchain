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
            var method = new DynamicMethodConvertableWrapperImpl();
            method.Make(
                $"LoadReferenceToLocalVar_{varName}",
                0,
                (il, context) =>
                {
                    var type = _variableTypes[varName] = context.Stack[0];
                    il.Ldstr(varName);
                    var variablesContainer = typeof(VariablesContainer<>).MakeGenericType(type);
                    var getRefMethod = variablesContainer.GetMethod("GetRef");
                    il.Call(getRefMethod);
                    il.Ret();
                },
                context => typeof(VariableReference<>).MakeGenericType(context.Stack[0])
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
        else
        {
            var method = new DynamicMethodConvertableWrapperImpl();
            method.Make($"LoadValueOfLocalVar_{varName}", 0,
                (il, _) =>
                {
                    il.Ldstr(varName);
                    il.Call(typeof(VariablesContainer<>).MakeGenericType(_variableTypes[varName]).GetMethod("Get"));
                    il.Ret();
                }, _ => _variableTypes[varName]);
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
    }
}