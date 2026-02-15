using AbstractIrExtensions;
using BasicCore;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using JetBrains.Annotations;
using IntermediateRepresentationAbstractions;
using System.Diagnostics;

namespace ConditionsModule;

[AutoRegisterService]
public class BooleanVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;
        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("True") ||
            nodeType == ExtensibleEnum<AstNodeTag>.Get("False"))
            VisitBooleanLiteral(data);
        else if (nodeType == ExtensibleEnum<AstNodeTag>.Get("And") ||
                 nodeType == ExtensibleEnum<AstNodeTag>.Get("Or"))
            VisitBooleanOperationWithShortCircuit(data);
        else if (nodeType == ExtensibleEnum<AstNodeTag>.Get("Not"))
            VisitBooleanOperation(data);
    }

    private void VisitBooleanLiteral(BytecodeVisitorData data)
    {
        var value = data.Node.NodeType == ExtensibleEnum<AstNodeTag>.Get("True");
        var method = new AbstractMethodImpl(
            $"PushBoolean_{value}",
            (il, _) => il.Push(value)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void VisitBooleanOperation(BytecodeVisitorData data)
    {
        foreach (var child in data.Node.Children)
            data.AstToBytecodeTranslator.Translate(child);

        var op = data.Node.NodeType;
        var method = new AbstractMethodImpl(
            $"Boolean_{op}",
            (il, context) =>
            {
                if (context.Stack[^1] != typeof(bool))
                    il.CallCSharp(context.Stack[^1].GetMethod(op.GetName()).NotNull());
                else
                    il.CallCSharp(typeof(BooleanOperations).GetMethod(op.GetName()).NotNull());
            }
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void VisitBooleanOperationWithShortCircuit(BytecodeVisitorData data)
    {
        var labelsBefore = DebugMetrics.Labels;
        var condJumpsBefore = DebugMetrics.ConditionalJumps;
        var jumpsBefore = DebugMetrics.Jumps;

        var falseLabel = Guid.NewGuid();
        var trueLabel = Guid.NewGuid();
        var endLabel = Guid.NewGuid();

        EmitCond(data, data.Node, trueLabel, falseLabel);

        EmitLabel(data, $"BoolTrueLabel_{trueLabel}", trueLabel);
        EmitInstruction(data, $"PushBoolean_true_{trueLabel}", (il, _) => il.Push(true));
        EmitJump(data, $"BoolJumpEnd_{endLabel}", endLabel);

        EmitLabel(data, $"BoolFalseLabel_{falseLabel}", falseLabel);
        EmitInstruction(data, $"PushBoolean_false_{falseLabel}", (il, _) => il.Push(false));

        EmitLabel(data, $"BoolEndLabel_{endLabel}", endLabel);

        DebugLogMetrics(labelsBefore, condJumpsBefore, jumpsBefore);
    }

    /// <summary>
    ///     Генерирует булево выражение через управление потоком без materialize bool на стеке.
    ///     Переходит в trueLabel если node истинно, иначе в falseLabel.
    /// </summary>
    private void EmitCond(BytecodeVisitorData data, AstNode node, Guid trueLabel, Guid falseLabel)
    {
        var nodeType = node.NodeType;

        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("And"))
        {
            // A && B:
            // EmitCond(A, mid, false)
            // mid:
            // EmitCond(B, true, false)
            var midLabel = Guid.NewGuid();
            EmitCond(data, node.Children[0], midLabel, falseLabel);
            EmitLabel(data, $"BoolAndMidLabel_{midLabel}", midLabel);
            EmitCond(data, node.Children[1], trueLabel, falseLabel);
            return;
        }

        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("Or"))
        {
            // A || B:
            // EmitCond(A, true, mid)
            // mid:
            // EmitCond(B, true, false)
            var midLabel = Guid.NewGuid();
            EmitCond(data, node.Children[0], trueLabel, midLabel);
            EmitLabel(data, $"BoolOrMidLabel_{midLabel}", midLabel);
            EmitCond(data, node.Children[1], trueLabel, falseLabel);
            return;
        }

        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("Not"))
        {
            // !A == swap(true, false)
            EmitCond(data, node.Children[0], falseLabel, trueLabel);
            return;
        }

        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("True"))
        {
            EmitJump(data, $"BoolConstTrueJump_{trueLabel}", trueLabel);
            return;
        }

        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("False"))
        {
            EmitJump(data, $"BoolConstFalseJump_{falseLabel}", falseLabel);
            return;
        }

        // Базовый случай: вычисляем bool-значение на стеке ровно один раз,
        // затем делаем один условный и один безусловный переход.
        data.AstToBytecodeTranslator.Translate(node);
        EmitConditionalJump(data, $"BoolCondTrueJump_{trueLabel}", trueLabel, jumpIfTrue: true);
        EmitJump(data, $"BoolCondFalseJump_{falseLabel}", falseLabel);
    }

    private void EmitInstruction(BytecodeVisitorData data, string name, Action<IntermediateRepresentationAbstractions.IAbstractIR, IAbstractMethodConvertable.Context> emit)
    {
        var method = new AbstractMethodImpl(name, emit);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void EmitLabel(BytecodeVisitorData data, string name, Guid label)
    {
        EmitInstruction(data, name, (il, _) => il.SetLabel(label));
        DebugMetrics.Labels++;
    }

    private void EmitJump(BytecodeVisitorData data, string name, Guid label)
    {
        EmitInstruction(data, name, (il, _) => il.Jmp(label));
        DebugMetrics.Jumps++;
    }

    private void EmitConditionalJump(BytecodeVisitorData data, string name, Guid label, bool jumpIfTrue)
    {
        EmitInstruction(
            data,
            name,
            (il, _) =>
            {
                if (jumpIfTrue)
                    il.JmpIf(label);
                else
                    il.JmpIfNot(label);
            });
        DebugMetrics.ConditionalJumps++;
    }

    [Conditional("DEBUG")]
    private static void DebugLogMetrics(int labelsBefore, int condJumpsBefore, int jumpsBefore)
    {
        Debug.WriteLine(
            $"[BooleanVisitor] labels={DebugMetrics.Labels - labelsBefore}, condJumps={DebugMetrics.ConditionalJumps - condJumpsBefore}, jumps={DebugMetrics.Jumps - jumpsBefore}");
    }

    private static class DebugMetrics
    {
        public static int Labels;
        public static int ConditionalJumps;
        public static int Jumps;
    }

    [UsedImplicitly]
    public static class BooleanOperations
    {
        [UsedImplicitly]
        public static bool And(bool a, bool b) => a && b;

        [UsedImplicitly]
        public static bool Or(bool a, bool b) => a || b;

        [UsedImplicitly]
        public static bool Not(bool a) => !a;
    }
}
