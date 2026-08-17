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
    private void LowerOperation(WistSemanticOperationNode operation, Bytecode bytecode)
    {
        if (operation.Operation == WistSemanticOperations.Add)
        {
            RequireContribution(WistContributionIds.CanonicalAddLowering);
            LowerOperands(operation, bytecode);
            EmitDynamicArithmetic(bytecode, "Op_Add", "Add");
            return;
        }

        if (operation.Operation == WistSemanticOperations.Subtract)
        {
            RequireModule(WistContributionIds.ArithmeticModule);
            LowerOperands(operation, bytecode);
            EmitDynamicArithmetic(bytecode, "Op_-", "Sub");
            return;
        }

        if (operation.Operation == WistSemanticOperations.Multiply)
        {
            RequireModule(WistContributionIds.ArithmeticModule);
            LowerOperands(operation, bytecode);
            EmitDynamicArithmetic(bytecode, "Op_*", "Mul");
            return;
        }

        if (operation.Operation == WistSemanticOperations.Divide)
        {
            RequireModule(WistContributionIds.ArithmeticModule);
            LowerOperands(operation, bytecode);
            EmitDynamicArithmetic(bytecode, "Op_/", "Div");
            return;
        }

        if (operation.Operation == WistSemanticOperations.UnaryMinus)
        {
            RequireModule(WistContributionIds.ArithmeticModule);
            RequireModule(WistContributionIds.NumbersModule);
            if (operation.Children.Count != 1)
                throw new InvalidOperationException("Wist unary minus requires exactly one operand.");
            EmitRealNumber(bytecode, 0d);
            LowerNode(operation.Children[0], bytecode);
            EmitDynamicArithmetic(bytecode, "Op_-", "Sub");
            return;
        }

        if (operation.Operation == WistSemanticOperations.NativeAdd)
        {
            LowerNativeBinary(operation, bytecode, "Add");
            return;
        }
        if (operation.Operation == WistSemanticOperations.NativeSubtract)
        {
            LowerNativeBinary(operation, bytecode, "Subtract");
            return;
        }
        if (operation.Operation == WistSemanticOperations.NativeMultiply)
        {
            LowerNativeBinary(operation, bytecode, "Multiply");
            return;
        }
        if (operation.Operation == WistSemanticOperations.NativeDivide)
        {
            LowerNativeBinary(operation, bytecode, "Divide");
            return;
        }
        if (operation.Operation == WistSemanticOperations.NativeUnaryMinus)
        {
            RequireModule(WistContributionIds.NativeTypesModule);
            if (operation.Children.Count != 1)
                throw new InvalidOperationException("Wist native unary minus requires exactly one operand.");
            LowerNode(operation.Children[0], bytecode);
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                "NativeUnaryMinus_Negate",
                static (il, context) =>
                {
                    var operandType = context.Stack[^1];
                    il.CallCSharp(ResolveNativeUnaryMinusMethod(operandType));
                })));
            return;
        }

        var comparison = GetComparison(operation.Operation);
        if (comparison != null)
        {
            RequireModule(WistContributionIds.ComparisonsModule);
            if (operation.Children.Count != 2)
                throw new InvalidOperationException($"Comparison '{operation.Operation.Value}' requires exactly two operands.");
            LowerOperands(operation, bytecode);
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                $"Comparison_{comparison.Value.Symbol}",
                (il, _) => il.CallCSharp(typeof(Comparisons).GetMethod(comparison.Value.MethodName).NotNull()))));
            return;
        }

        if (operation.Operation == WistSemanticOperations.BooleanNot)
        {
            RequireModule(WistContributionIds.BooleanLogicModule);
            if (operation.Children.Count != 1)
                throw new InvalidOperationException("Boolean not requires exactly one operand.");
            LowerNode(operation.Children[0], bytecode);
            bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
                "Boolean_Not",
                static (il, context) =>
                {
                    if (context.Stack[^1] != typeof(bool))
                        il.CallCSharp(context.Stack[^1].GetMethod("Not").NotNull());
                    else
                        il.CallCSharp(typeof(WistBooleanRuntimeOperations).GetMethod(nameof(WistBooleanRuntimeOperations.Not)).NotNull());
                })));
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported Wist semantic operation '{operation.Operation.Value}'. Native lowering fails closed.");
    }

    private void LowerOperands(WistSemanticOperationNode operation, Bytecode bytecode)
    {
        foreach (var child in operation.Children)
            LowerNode(child, bytecode);
    }

    private static void EmitDynamicArithmetic(Bytecode bytecode, string instructionName, string methodName)
    {
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            instructionName,
            (il, context) => il.CallCSharp(context.Stack[^1].GetMethod(methodName).NotNull())));
    }

    private void LowerNativeBinary(WistSemanticOperationNode operation, Bytecode bytecode, string methodName)
    {
        RequireModule(WistContributionIds.NativeTypesModule);
        if (operation.Children.Count != 2)
            throw new InvalidOperationException($"Native arithmetic '{methodName}' requires exactly two operands.");
        LowerOperands(operation, bytecode);
        bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"NativeArithmetic_{methodName}",
            (il, context) =>
                il.CallCSharp(ResolveNativeArithmeticMethod(methodName, context.Stack[^2], context.Stack[^1])))));
    }

    private static MethodInfo ResolveNativeArithmeticMethod(string methodName, Type leftType, Type rightType)
    {
        if (leftType != rightType)
            throw new InvalidOperationException(
                $"Native arithmetic requires matching operand types. Left='{leftType}', right='{rightType}'.");

        if (leftType == typeof(decimal))
        {
            return typeof(NativeArithmetic)
                .GetMethod(methodName + "Decimal", BindingFlags.Static | BindingFlags.Public)
                .NotNull();
        }

        return typeof(NativeArithmetic)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)
            .NotNull()
            .MakeGenericMethod(leftType);
    }

    private static MethodInfo ResolveNativeUnaryMinusMethod(Type operandType)
    {
        if (operandType == typeof(decimal))
        {
            return typeof(NativeArithmetic)
                .GetMethod(nameof(NativeArithmetic.NegateDecimal), BindingFlags.Static | BindingFlags.Public)
                .NotNull();
        }

        try
        {
            return typeof(NativeArithmetic)
                .GetMethod(nameof(NativeArithmetic.Negate), BindingFlags.Static | BindingFlags.Public)
                .NotNull()
                .MakeGenericMethod(operandType);
        }
        catch (Exception)
        {
            return Thrower.NotSupported<MethodInfo>(
                $"Native unary minus does not support operand type '{operandType}'.");
        }
    }

    private static (string Symbol, string MethodName)? GetComparison(WistSemanticOperationId operation)
    {
        if (operation == WistSemanticOperations.Equal) return ("==", "Equal");
        if (operation == WistSemanticOperations.NotEqual) return ("!=", "NotEqual");
        if (operation == WistSemanticOperations.Greater) return (">", "Greater");
        if (operation == WistSemanticOperations.Less) return ("<", "Less");
        if (operation == WistSemanticOperations.GreaterOrEqual) return (">=", "GreaterOrEqual");
        if (operation == WistSemanticOperations.LessOrEqual) return ("<=", "LessOrEqual");
        return null;
    }
}
