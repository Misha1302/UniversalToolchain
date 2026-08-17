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
    private static void EmitRealNumber(Bytecode bytecode, double value)
    {
        var method = new AbstractMethodImpl(
            $"PushNumber_{value}",
            (il, _) =>
            {
                il.Push(value);
                il.CallCSharp(typeof(RealNumberImpl).GetConstructor([typeof(double)]).NotNull());
            });
        bytecode.Instructions.Add(new BytecodeInstruction(method).WithContract(
            NumbersContractIds.Module,
            NumbersContractIds.NumberNode,
            NumbersContractIds.PushRealNumber));
    }

    private static void EmitNativeNumber(Bytecode bytecode, WistNativeLiteralValue literal)
    {
        var value = literal.Materialize();
        var valueType = value.GetType();
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"PushNative_{valueType.Name}_{value}",
            (il, _) => il.Push(value))));
    }

    private static void EmitBooleanLiteral(Bytecode bytecode, bool value)
    {
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"PushBoolean_{value}",
            (il, _) => il.Push(value))));
    }

    private void EmitVariable(Bytecode bytecode, WistSymbolReferenceNode reference)
    {
        var symbol = reference.Symbol;
        var symbolType = symbol.Type.Resolve();
        if (symbol.Kind == WistSemanticSymbolKind.Local && IsConcreteType(symbolType))
            _variablesTypes[symbol.StorageKey] = symbolType;

        if (reference.IsWriteTarget)
        {
            if (!symbol.CanAssign)
                Thrower.InvalidOpEx($"External constant '{symbol.Name}' cannot be assigned.");

            EmitWriteTypeInference(bytecode, symbol, symbolType);
            return;
        }

        if (symbol.Kind == WistSemanticSymbolKind.Local)
        {
            var loadMethod = new AbstractMethodImpl(
                $"LoadValueOfLocalVar_{symbol.Name}",
                (il, _) => il.LdLoc(symbol.StorageKey, ResolveReadType(symbol, symbolType)));
            bytecode.Instructions.Add(new BytecodeInstruction(loadMethod).WithContract(
                VariablesContractIds.Module,
                VariablesContractIds.VariableNode,
                VariablesContractIds.LocalRead));
            return;
        }

        var loadExternalMethod = new AbstractMethodImpl(
            $"LoadValueOfExternalVar_{symbol.Name}",
            (il, _) =>
            {
                var loadType = _variablesTypes.TryGetValue(symbol.Name, out var refinedType) && IsConcreteType(refinedType)
                    ? refinedType
                    : symbolType;
                il.LdExternal(symbol.ExternalSlot, loadType);
            });
        bytecode.Instructions.Add(new BytecodeInstruction(loadExternalMethod).WithContract(
            VariablesContractIds.Module,
            VariablesContractIds.VariableNode,
            VariablesContractIds.ExternalRead));
    }

    private void EmitWriteTypeInference(Bytecode bytecode, WistSemanticSymbolId symbol, Type symbolType)
    {
        if (symbol.Kind == WistSemanticSymbolKind.Local)
        {
            var inferMethod = new AbstractMethodImpl(
                $"InferWriteTypeOfLocalVar_{symbol.Name}",
                (_, context) =>
                {
                    if (context.Stack.Count == 0)
                        Thrower.InvalidOpEx($"Cannot infer storage type for local variable '{symbol.Name}' without assignment value.");
                    UpdateWriteType(symbol.StorageKey, symbolType, context.Stack[^1]);
                });
            bytecode.Instructions.Add(new BytecodeInstruction(inferMethod).WithContract(
                VariablesContractIds.Module,
                VariablesContractIds.VariableNode,
                VariablesContractIds.WriteTypeInference,
                VariablesContractIds.WriteTargetTypeInference));
            return;
        }

        var externalInferMethod = new AbstractMethodImpl(
            $"InferWriteTypeOfExternalVar_{symbol.Name}",
            (_, context) =>
            {
                if (context.Stack.Count == 0)
                    Thrower.InvalidOpEx($"Cannot infer storage type for external variable '{symbol.Name}' without assignment value.");
                var inferredType = context.Stack[^1];
                _variablesTypes[symbol.Name] = IsConcreteType(inferredType) ? inferredType : symbolType;
            });
        bytecode.Instructions.Add(new BytecodeInstruction(externalInferMethod).WithContract(
            VariablesContractIds.Module,
            VariablesContractIds.VariableNode,
            VariablesContractIds.WriteTypeInference,
            VariablesContractIds.WriteTargetTypeInference));
    }

    private Type ResolveReadType(WistSemanticSymbolId symbol, Type symbolType)
    {
        if (_variablesTypes.TryGetValue(symbol.StorageKey, out var existing))
        {
            if (IsConcreteType(existing))
                return existing;
            if (symbol.Kind is WistSemanticSymbolKind.ExternalVariable or WistSemanticSymbolKind.ExternalConstant)
                return existing;
        }

        if (IsConcreteType(symbolType))
        {
            _variablesTypes[symbol.StorageKey] = symbolType;
            return symbolType;
        }

        Thrower.InvalidOpEx(
            $"Storage type for variable '{symbol.Name}' is not fixed before read. " +
            $"Current symbol type: '{symbolType.FullName}'.");
        return null!;
    }

    private void UpdateWriteType(string variableKey, Type symbolType, Type inferredType)
    {
        if (_variablesTypes.TryGetValue(variableKey, out var existing) && IsConcreteType(existing))
            return;
        if (IsConcreteType(symbolType))
        {
            _variablesTypes[variableKey] = symbolType;
            return;
        }
        _variablesTypes[variableKey] = IsConcreteType(inferredType) ? inferredType : typeof(object);
    }

    private static bool IsConcreteType(Type type) => type != typeof(object);
}
