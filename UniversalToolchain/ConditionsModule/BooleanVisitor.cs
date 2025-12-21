using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace ConditionsModule;

public class BooleanVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;
        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("True") ||
            nodeType == ExtensibleEnum<AstNodeTag>.Get("False"))
            VisitBooleanLiteral(data);
        else if (nodeType == ExtensibleEnum<AstNodeTag>.Get("And") ||
                 nodeType == ExtensibleEnum<AstNodeTag>.Get("Or") ||
                 nodeType == ExtensibleEnum<AstNodeTag>.Get("Not"))
            VisitBooleanOperation(data);
    }

    private void VisitBooleanLiteral(BytecodeVisitorData data)
    {
        var value = data.Node.NodeType == ExtensibleEnum<AstNodeTag>.Get("True");
        var method = new DynamicMethodConvertableWrapperImpl();

        method.Make($"PushBoolean_{value}", 0, (il, _) =>
            {
                il.Ldc_I4(value ? 1 : 0);
                il.Ret();
            }, _ => typeof(bool)
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void VisitBooleanOperation(BytecodeVisitorData data)
    {
        // Сначала вычисляем операнды
        foreach (var child in data.Node.Children) data.BytecodeTranslator.Translate(child);

        var method = new DynamicMethodConvertableWrapperImpl();
        var op = data.Node.NodeType;

        method.Make($"Boolean_{op}",
            data.Node.Children.Count == 1 ? 1 : 2,
            (il, _) =>
            {
                if (op == ExtensibleEnum<AstNodeTag>.Get("And"))
                {
                    il.Ldarg(0);
                    il.Ldarg(1);
                    il.And();
                }
                else if (op == ExtensibleEnum<AstNodeTag>.Get("Or"))
                {
                    il.Ldarg(0);
                    il.Ldarg(1);
                    il.Or();
                }
                else if (op == ExtensibleEnum<AstNodeTag>.Get("Not"))
                {
                    il.Ldarg(0);
                    il.Ldc_I4(0);
                    il.Ceq();
                }
                else
                {
                    Thrower.InvalidOpEx($"Unknown operator {op}");
                }

                il.Ret();
            }, _ => typeof(bool)
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}