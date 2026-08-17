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
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.ModuleContracts;
using VariablesModule.Contracts;

namespace UniversalToolchain.Wist.LanguagePack;

internal sealed partial class WistSemanticBytecodeLowerer
{
    private readonly HashSet<LanguageContributionId> _plannedContributions;
    private readonly FunctionCallPlanner _functionCallPlanner;
    private readonly IMethodResolver _methodResolver;
    private readonly ITypeCatalog _typeCatalog;
    private readonly OrderedDictionary<string, Type> _variablesTypes = [];
    private readonly LabelsSharedData _labels = new();
    private readonly HashSet<string> _markedLabels = [];
    private int _callSequence;
    private int _ifExpressionSequence;

    public WistSemanticBytecodeLowerer(
        LanguagePlan plan,
        CapabilityCatalog capabilityCatalog,
        IMethodResolver methodResolver,
        ITypeCatalog typeCatalog)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(capabilityCatalog);
        _methodResolver = methodResolver ?? throw new ArgumentNullException(nameof(methodResolver));
        _typeCatalog = typeCatalog ?? throw new ArgumentNullException(nameof(typeCatalog));
        _plannedContributions = plan.Contributions
            .Select(static contribution => contribution.Contribution.Id)
            .ToHashSet();
        _functionCallPlanner = new FunctionCallPlanner(capabilityCatalog.BuiltinFunctionRuntimeBindings);
    }

    public static bool SupportsModuleContribution(LanguageContributionId contributionId) =>
        contributionId == WistContributionIds.ScopesModule
        || contributionId == WistContributionIds.NumbersModule
        || contributionId == WistContributionIds.ArithmeticModule
        || contributionId == WistContributionIds.VariablesModule
        || contributionId == WistContributionIds.ComparisonsModule
        || contributionId == WistContributionIds.BooleanLogicModule
        || contributionId == WistContributionIds.ConditionalControlFlowModule
        || contributionId == WistContributionIds.CSharpInteropModule
        || contributionId == WistContributionIds.EqualityModule
        || contributionId == WistContributionIds.FunctionCallsModule
        || contributionId == WistContributionIds.LabelsModule
        || contributionId == WistContributionIds.LoopsModule
        || contributionId == WistContributionIds.NativeTypesModule;

    public Bytecode Lower(WistSemanticProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        ResetRequestState();
        var bytecode = new Bytecode([]);
        LowerNode(program.Root, bytecode);
        return bytecode;
    }

    private void ResetRequestState()
    {
        _variablesTypes.Clear();
        _markedLabels.Clear();
        _callSequence = 0;
        _ifExpressionSequence = 0;
    }

    private void LowerNode(WistSemanticNode node, Bytecode bytecode)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(bytecode);

        switch (node)
        {
            case WistSemanticSequenceNode sequence:
                LowerSequence(sequence, bytecode);
                return;
            case WistNumberNode number:
                RequireModule(WistContributionIds.NumbersModule);
                EmitRealNumber(bytecode, number.Value);
                return;
            case WistNativeNumberNode nativeNumber:
                RequireModule(WistContributionIds.NativeTypesModule);
                EmitNativeNumber(bytecode, nativeNumber.Value);
                return;
            case WistBooleanLiteralNode boolean:
                RequireModule(WistContributionIds.BooleanLogicModule);
                EmitBooleanLiteral(bytecode, boolean.Value);
                return;
            case WistSymbolReferenceNode symbol:
                RequireModule(WistContributionIds.VariablesModule);
                EmitVariable(bytecode, symbol);
                return;
            case WistSemanticOperationNode operation:
                LowerOperation(operation, bytecode);
                return;
            case WistAssignmentNode assignment:
                LowerAssignment(assignment, bytecode);
                return;
            case WistShortCircuitNode shortCircuit:
                LowerShortCircuit(shortCircuit, bytecode);
                return;
            case WistConditionalBranchNode conditional:
                LowerConditional(conditional, bytecode);
                return;
            case WistElseNode @else:
                RequireModule(WistContributionIds.ConditionalControlFlowModule);
                LowerNode(@else.Body, bytecode);
                return;
            case WistIfExpressionNode ifExpression:
                LowerIfExpression(ifExpression, bytecode);
                return;
            case WistWhileNode @while:
                LowerWhile(@while, bytecode);
                return;
            case WistForNode @for:
                LowerFor(@for, bytecode);
                return;
            case WistLabelNode label:
                LowerLabel(label, bytecode);
                return;
            case WistGotoNode @goto:
                LowerGoto(@goto, bytecode);
                return;
            case WistFunctionCallNode functionCall:
                LowerFunctionCall(functionCall, bytecode);
                return;
            case WistCSharpCallNode csharpCall:
                LowerCSharpCall(csharpCall, bytecode);
                return;
            case WistDefineArgumentNode defineArgument:
                LowerDefineArgument(defineArgument, bytecode);
                return;
            default:
                throw new InvalidOperationException(
                    $"Unsupported Wist semantic node '{node.GetType().FullName}'. Native lowering fails closed.");
        }
    }

    private void LowerSequence(WistSemanticSequenceNode sequence, Bytecode bytecode)
    {
        if (sequence.Kind == WistSemanticSequenceKind.Scope)
            RequireModule(WistContributionIds.ScopesModule);
        foreach (var child in sequence.Children)
            LowerNode(child, bytecode);
    }
}
