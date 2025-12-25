using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using JetBrains.Annotations;
using UniversalIntermediateRepresentation;

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
        var method = new AbstractMethodImpl(
            $"PushBoolean_{value}",
            0,
            (il, _) => il.Push(Value.Create(value)),
            _ => typeof(bool)
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void VisitBooleanOperation(BytecodeVisitorData data)
    {
        // Сначала вычисляем операнды
        foreach (var child in data.Node.Children) data.BytecodeTranslator.Translate(child);

        var op = data.Node.NodeType;


        var method = new AbstractMethodImpl(
            $"Boolean_{op}",
            data.Node.Children.Count == 1 ? 1 : 2,
            (il, context) =>
            {
                // args always pushed
                Thrower.AssertAlways(op.GetName() is "And" or "Or" or "Not");
                Thrower.AssertAlways(context.Stack[^1] == context.Stack[^2]);
                if (context.Stack[^1] != typeof(bool))
                    il.CallCSharp(context.Stack[^1].GetMethod(op.GetName()).NotNull());
                else il.CallCSharp(typeof(BooleanOperations).GetMethod(op.GetName()).NotNull());
            },
            _ => typeof(bool)
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    [UsedImplicitly]
    private static class BooleanOperations
    {
        [UsedImplicitly]
        public static bool And(bool a, bool b)
        {
            return a && b;
        }

        [UsedImplicitly]
        public static bool Or(bool a, bool b)
        {
            return a || b;
        }

        [UsedImplicitly]
        public static bool Not(bool a)
        {
            return !a;
        }
    }
}