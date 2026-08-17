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
    private static void RequireBooleanCondition(IAbstractMethodConvertable.Context context)
    {
        if (context.Stack.Count == 0)
            Thrower.InvalidOpEx("IfExpression condition must leave a value on the stack.");
        var conditionType = context.Stack[^1];
        if (conditionType != typeof(bool))
            Thrower.InvalidOpEx(
                $"IfExpression condition must be boolean. Actual type: '{conditionType.FullName}'.");
    }

    private static void StoreBranchResult(
        IAbstractIR il,
        IAbstractMethodConvertable.Context context,
        string resultLocalName,
        ref Type? resultType)
    {
        if (context.Stack.Count == 0)
            Thrower.InvalidOpEx("IfExpression branch must leave a value on the stack.");
        var branchType = context.Stack[^1];
        if (resultType == null)
            resultType = branchType;
        else if (resultType != branchType)
            Thrower.InvalidOpEx(
                $"IfExpression branch types must match. Expected '{resultType.FullName}', actual '{branchType.FullName}'.");
        il.SetValueToLocal(resultLocalName, branchType);
    }

    private void LowerWhile(WistWhileNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.LoopsModule);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"WhileStart_!Intrinsic_{node.StartLabel}",
            (il, _) => il.SetLabel(node.StartLabel))));
        LowerNode(node.Condition, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"WhileExit_!Intrinsic_{node.EndLabel}",
            (il, _) => il.JmpIfNot(node.EndLabel))));
        LowerNode(node.Body, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"WhileBack_!Intrinsic_{node.StartLabel}",
            (il, _) => il.Jmp(node.StartLabel))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"WhileEnd_!Intrinsic_{node.EndLabel}",
            (il, _) => il.SetLabel(node.EndLabel))));
    }

    private void LowerFor(WistForNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.LoopsModule);
        LowerNode(node.Initialization, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"ForStart_!Intrinsic_{node.StartLabel}",
            (il, _) => il.SetLabel(node.StartLabel))));
        LowerNode(node.Condition, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"ForExit_!Intrinsic_{node.EndLabel}",
            (il, _) => il.JmpIfNot(node.EndLabel))));
        LowerNode(node.Body, bytecode);
        LowerNode(node.Step, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"ForBack_!Intrinsic_{node.StartLabel}",
            (il, _) => il.Jmp(node.StartLabel))));
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"ForEnd_!Intrinsic_{node.EndLabel}",
            (il, _) => il.SetLabel(node.EndLabel))));
    }

    private void LowerLabel(WistLabelNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.LabelsModule);
        if (!_markedLabels.Add(node.Name))
            Thrower.MultipleDefinition($"label '{node.Name}''");
        var method = new AbstractMethodImpl(
            $"Label_!Intrinsic_{node.Name}",
            (il, _) => il.SetLabel(_labels.GetGuidByName(node.Name)));
        bytecode.Instructions.Add(new BytecodeInstruction(method).WithContract(
            LabelsContractIds.Module,
            LabelsContractIds.LabelNode,
            LabelsContractIds.Label));
    }

    private void LowerGoto(WistGotoNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.LabelsModule);
        var method = new AbstractMethodImpl(
            $"Goto_!Intrinsic_{node.Name}",
            (il, _) => il.Jmp(_labels.GetIdByName(node.Name)));
        bytecode.Instructions.Add(new BytecodeInstruction(method).WithContract(
            LabelsContractIds.Module,
            LabelsContractIds.GotoNode,
            LabelsContractIds.Goto));
    }

}
