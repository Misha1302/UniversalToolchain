// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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

        if (data.Node.AllTags.Contains("VariableDefinition"))
            _variableTypes[varName] = data.Node.Data.Get<Type>("Type");

        if (data.Node.AllTags.Contains("ExpectingSettableReference"))
        {
            var method = new DynamicMethodConvertableWrapperImpl();
            method.Make(
                $"LoadReferenceToLocalVar_{varName}",
                typeof(VariableReference<>).MakeGenericType(_variableTypes[varName]),
                [],
                (il, _) =>
                {
                    il.Ldstr(varName);
                    var variablesContainer = typeof(VariablesContainer<>).MakeGenericType(_variableTypes[varName]);
                    var getRefMethod = variablesContainer.GetMethod("GetRef");
                    il.Call(getRefMethod);
                    il.Ret();
                }
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
        else
        {
            var method = new DynamicMethodConvertableWrapperImpl();
            method.Make($"LoadValueOfLocalVar_{varName}", _variableTypes[varName], [],
                (il, _) =>
                {
                    il.Ldstr(varName);
                    il.Call(typeof(VariablesContainer<>).MakeGenericType(_variableTypes[varName]).GetMethod("Get"));
                    il.Ret();
                }
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
    }
}