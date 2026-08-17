using System.Reflection;
using BasicCore.Capabilities;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using ConditionsModule.Enums;
using CommonExceptions;
using ExceptionsManager;
using FunctionCallsModule;
using LabelsModule.Contracts;
using LabelsModule.Core;
using IntermediateRepresentationAbstractions;
using NativeMathModule;
using NumbersModule.Contracts;
using NumbersModule.Core;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.ModuleContracts;
using VariablesModule.Contracts;

namespace UniversalToolchain.Wist.LanguagePack;

internal sealed partial class WistSemanticBytecodeLowerer
{
    private void LowerAssignment(WistAssignmentNode assignment, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.EqualityModule);
        LowerNode(assignment.Value, bytecode);
        LowerNode(assignment.Target, bytecode);

        var target = assignment.Target.Symbol;
        var targetType = target.Type.Resolve();
        var method = new AbstractMethodImpl(
            $"Set_{target.Name}",
            (il, context) =>
            {
                if (context.Stack.Count == 0)
                    Thrower.InvalidOpEx("Assignment requires a value on the stack.");
                if (target.Kind == WistSemanticSymbolKind.ExternalConstant)
                    Thrower.InvalidOpEx($"External constant '{target.Name}' cannot be assigned.");
                if (target.Kind == WistSemanticSymbolKind.ExternalVariable)
                {
                    il.StExternal(target.ExternalSlot, targetType);
                    return;
                }
                il.SetValueToLocal(target.StorageKey, context.Stack[^1]);
            });
        bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void LowerShortCircuit(WistShortCircuitNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.BooleanLogicModule);
        LowerNode(node.Left, bytecode);
        if (node.IsAnd)
        {
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                $"BoolCondJump_And_{node.FalseLabel}",
                (il, _) => il.JmpIfNot(node.FalseLabel))));
        }
        else
        {
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                $"BoolCondJump_Or_{node.TrueLabel}",
                (il, _) => il.JmpIf(node.TrueLabel))));
        }

        LowerNode(node.Right, bytecode);
        if (node.IsAnd)
        {
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                $"BoolAndRightFalse_{node.FalseLabel}",
                (il, _) => il.JmpIfNot(node.FalseLabel))));
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                $"BoolAndJumpTrue_{node.TrueLabel}",
                (il, _) => il.Jmp(node.TrueLabel))));
        }
        else
        {
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                $"BoolOrRightTrue_{node.TrueLabel}",
                (il, _) => il.JmpIf(node.TrueLabel))));
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                $"BoolOrJumpFalse_{node.FalseLabel}",
                (il, _) => il.Jmp(node.FalseLabel))));
        }

        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"BoolFalseLabel_{node.FalseLabel}",
            (il, _) => il.SetLabel(node.FalseLabel))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"PushBoolean_false_{node.FalseLabel}",
            (il, _) => il.Push(false))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"BoolJumpEnd_{node.EndLabel}",
            (il, _) => il.Jmp(node.EndLabel))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"BoolTrueLabel_{node.TrueLabel}",
            (il, _) => il.SetLabel(node.TrueLabel))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"PushBoolean_true_{node.TrueLabel}",
            (il, _) => il.Push(true))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"BoolEndLabel_{node.EndLabel}",
            (il, _) => il.SetLabel(node.EndLabel))));
    }

    private void LowerConditional(WistConditionalBranchNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.ConditionalControlFlowModule);
        LowerNode(node.Condition, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"CondFGoto_!Intrinsic_{node.ElseLabel}",
            (il, _) => il.JmpIfNot(node.ElseLabel))));
        LowerNode(node.Body, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"Goto_!Intrinsic_{node.EndLabel}",
            (il, _) => il.Jmp(node.EndLabel))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"Label_!Intrinsic_{node.ElseLabel}",
            (il, _) => il.SetLabel(node.ElseLabel))));
        foreach (var alternative in node.Alternatives)
            LowerNode(alternative, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"Label_!Intrinsic_{node.EndLabel}",
            (il, _) => il.SetLabel(node.EndLabel))));
    }

    private void LowerIfExpression(WistIfExpressionNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.ConditionalControlFlowModule);
        var sequence = _ifExpressionSequence++;
        var falseLabel = CreateIfExpressionLabel(sequence, 1);
        var endLabel = CreateIfExpressionLabel(sequence, 2);
        var resultLocalName = $"__if_expression_result_{sequence}";
        Type? resultType = null;

        LowerNode(node.Condition, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_RequireBooleanCondition_{sequence}",
            (_, context) => RequireBooleanCondition(context))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_JmpIfNot_{falseLabel}",
            (il, _) => il.JmpIfNot(falseLabel))));

        LowerNode(node.WhenTrue, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_StoreTrueResult_{resultLocalName}",
            (il, context) => StoreBranchResult(il, context, resultLocalName, ref resultType))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Jmp_{endLabel}",
            (il, _) => il.Jmp(endLabel))));

        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Label_{falseLabel}",
            (il, _) => il.SetLabel(falseLabel))));
        LowerNode(node.WhenFalse, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_StoreFalseResult_{resultLocalName}",
            (il, context) => StoreBranchResult(il, context, resultLocalName, ref resultType))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Label_{endLabel}",
            (il, _) => il.SetLabel(endLabel))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_LoadResult_{resultLocalName}",
            (il, _) => il.LdLoc(resultLocalName, resultType.NotNull()))));
    }

    private static Guid CreateIfExpressionLabel(int sequence, byte marker)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(sequence).CopyTo(bytes, 0);
        bytes[15] = marker;
        return new Guid(bytes);
    }
}
