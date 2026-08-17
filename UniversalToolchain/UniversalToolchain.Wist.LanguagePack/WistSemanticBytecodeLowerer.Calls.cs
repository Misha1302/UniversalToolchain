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
    private void LowerFunctionCall(WistFunctionCallNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.FunctionCallsModule);
        foreach (var argument in node.Arguments)
            LowerNode(argument, bytecode);

        var callSequence = _callSequence++;
        var localPrefix = $"__function_call_{callSequence}";
        var argumentCount = node.Arguments.Count;
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"CallFunction_{node.FunctionName}_{argumentCount}",
            (il, context) => EmitFunctionCall(
                il,
                context.Stack.ToArray(),
                node.FunctionName,
                argumentCount,
                localPrefix))));
    }

    private void EmitFunctionCall(
        IAbstractIR il,
        IReadOnlyList<Type> stackTypes,
        string functionName,
        int argumentCount,
        string localPrefix)
    {
        if (stackTypes.Count < argumentCount)
            Thrower.InvalidOpEx($"Function call '{functionName}' requires {argumentCount} stack argument(s).");
        var sourceTypes = stackTypes.TakeLast(argumentCount).ToList();
        var plan = _functionCallPlanner.PlanOrThrow(functionName, sourceTypes);
        var localNames = Enumerable.Range(0, argumentCount)
            .Select(index => $"{localPrefix}_arg_{index}")
            .ToList();
        for (var index = argumentCount - 1; index >= 0; index--)
            il.SetValueToLocal(localNames[index], sourceTypes[index]);
        for (var index = 0; index < argumentCount; index++)
        {
            il.LdLoc(localNames[index], sourceTypes[index]);
            var adapter = plan.ArgumentAdapters[index];
            if (adapter != null)
                il.CallCSharp(adapter);
        }
        il.CallCSharp(plan.Binding.Method);
        if (plan.ResultAdapterFactory != null)
            il.CallCSharp(plan.ResultAdapterFactory);
        else if (plan.ResultAdapterConstructor != null)
            il.CallCSharp(plan.ResultAdapterConstructor);
    }

    private void LowerCSharpCall(WistCSharpCallNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.CSharpInteropModule);
        foreach (var argument in node.Arguments)
            LowerNode(argument, bytecode);

        var argumentCount = node.Arguments.Count;
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"Call_{node.FullName}",
            (il, context) =>
            {
                var stackTypes = context.Stack.TakeLast(argumentCount).ToList();
                var methodInfo = _methodResolver.GetMethod(node.FullName, stackTypes)
                                 ?? _methodResolver.GetMethod(node.FullName, argumentCount)
                                 ?? _methodResolver.GetMethod(node.FullName);
                if (methodInfo == null)
                    ToolchainThrower.Import($"Method '{node.FullName}({argumentCount} args)' not found in imported assemblies.");
                il.CallCSharp(methodInfo.NotNull());
            })));
    }

    private void LowerDefineArgument(WistDefineArgumentNode node, Bytecode bytecode)
    {
        RequireModule(WistContributionIds.VariablesModule);
        var type = _typeCatalog.ResolveRequiredType(node.TypeName);
        _variablesTypes[node.Name] = type;
        var method = new AbstractMethodImpl(
            $"DefineArgument_{node.Name}_{type.FullName}",
            (_, _) => _variablesTypes[node.Name] = type);
        bytecode.Instructions.Add(new BytecodeInstruction(method).WithContract(
            VariablesContractIds.Module,
            VariablesContractIds.VariableNode,
            VariablesContractIds.DefineArgument));
    }

    private void RequireModule(LanguageContributionId syntaxContributionId)
    {
        if (!SupportsModuleContribution(syntaxContributionId))
            throw new InvalidOperationException(
                $"Wist module '{syntaxContributionId.Value}' has no native semantic lowering implementation.");
        RequireContribution(WistModulePhaseOwnership.LoweringContributionId(syntaxContributionId));
    }

    private void RequireContribution(LanguageContributionId contributionId)
    {
        if (!_plannedContributions.Contains(contributionId))
        {
            throw new InvalidOperationException(
                $"Wist semantic construct requires lowering contribution '{contributionId.Value}', " +
                "but it is not selected by the current LanguagePlan.");
        }
    }
}
