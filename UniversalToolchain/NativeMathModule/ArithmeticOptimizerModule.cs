using System.Reflection;
using BasicCore;
using BasicCore.Attributes;
using BasicCore.Contracts;
using DotnetAirHelper;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using JetBrains.Annotations;
using ListExtensions;
using ObjectExtensions;
using UniversalIntermediateRepresentation;

namespace NativeMathModule;

[AutoRegisterService]
[UsedImplicitly]
public class ArithmeticOptimizerModule : IIRProcessingModule
{
    private static readonly IReadOnlyList<string> _standardModuleIntrinsics =
    [
        "add_i32", "sub_i32", "mul_i32", "div_i32",
        "add_i64", "sub_i64", "mul_i64", "div_i64",
        "add_f32", "sub_f32", "mul_f32", "div_f32",
        "add_f64", "sub_f64", "mul_f64", "div_f64"
    ];

    private static readonly IReadOnlyList<string> _decimalModuleIntrinsics =
    [
        "add_decimal", "sub_decimal", "mul_decimal", "div_decimal"
    ];

    private bool _isDecimalsSupported;

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (_standardModuleIntrinsics.Any(x => !compiler.SupportedIntrinsics.Contains(x)))
            return current;
        _isDecimalsSupported = _decimalModuleIntrinsics.All(x => compiler.SupportedIntrinsics.Contains(x));

        InitializeAirTypes();
        current = OptimizeArithmetic(current);
        return current;
    }

    private void InitializeAirTypes()
    {
        // Регистрация интринсиков для целых чисел
        foreach (var type in new[] { "i32", "i64" })
        {
            AirTypes.TryRegisterIntrinsic($"add_{type}", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(GetTypeFromSuffix(type));
            });
            AirTypes.TryRegisterIntrinsic($"sub_{type}", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(GetTypeFromSuffix(type));
            });
            AirTypes.TryRegisterIntrinsic($"mul_{type}", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(GetTypeFromSuffix(type));
            });
            AirTypes.TryRegisterIntrinsic($"div_{type}", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(GetTypeFromSuffix(type));
            });
        }

        // Регистрация интринсиков для чисел с плавающей точкой
        foreach (var type in new[] { "f32", "f64" })
        {
            AirTypes.TryRegisterIntrinsic($"add_{type}", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(GetTypeFromSuffix(type));
            });
            AirTypes.TryRegisterIntrinsic($"sub_{type}", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(GetTypeFromSuffix(type));
            });
            AirTypes.TryRegisterIntrinsic($"mul_{type}", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(GetTypeFromSuffix(type));
            });
            AirTypes.TryRegisterIntrinsic($"div_{type}", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(GetTypeFromSuffix(type));
            });
        }

        if (_isDecimalsSupported)
        {
            AirTypes.TryRegisterIntrinsic("add_decimal", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(decimal));
            });
            AirTypes.TryRegisterIntrinsic("sub_decimal", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(decimal));
            });
            AirTypes.TryRegisterIntrinsic("mul_decimal", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(decimal));
            });
            AirTypes.TryRegisterIntrinsic("div_decimal", (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(decimal));
            });
        }
    }

    private IAbstractIR OptimizeArithmetic(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var context = new CompilationContext();

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            if (instruction.UOpCode == UOpCode.Intrinsic &&
                instruction.Operands.Count >= 2 &&
                (string)instruction.Operands[0] == "call C#")
            {
                var method = instruction.Operands[1].Get<MethodInfo>();

                if (method.DeclaringType == typeof(NativeArithmetic))
                {
                    var intrinsicName = GetIntrinsicName(method);
                    if (intrinsicName != null)
                    {
                        context.NewInstructions.Add(new Instruction(UOpCode.Intrinsic, [intrinsicName]));
                        continue;
                    }
                }
            }

            context.NewInstructions.Add(instruction);
        }

        var result = new AbstractIR();
        result.AppendInstructions(context.NewInstructions);
        return result;
    }

    private string GetIntrinsicName(MethodInfo method)
    {
        var typeMap = new Dictionary<Type, string>
        {
            [typeof(int)] = "i32",
            [typeof(long)] = "i64",
            [typeof(float)] = "f32",
            [typeof(double)] = "f64",
            [typeof(decimal)] = "decimal"
        };

        var opMap = new Dictionary<string, string>
        {
            ["Add"] = "add",
            ["Subtract"] = "sub",
            ["Multiply"] = "mul",
            ["Divide"] = "div"
        };

        string typeSuffix = null;
        string operation = null;

        // Обработка обобщенных методов (int, long, float, double)
        if (method.IsGenericMethod)
        {
            var genericType = method.GetGenericArguments()[0];
            if (typeMap.TryGetValue(genericType, out typeSuffix))
                operation = opMap.GetValueOrDefault(method.Name);
        }
        // Обработка методов для decimal
        else if (method.Name.EndsWith("Decimal"))
        {
            typeSuffix = "decimal";
            operation = opMap.GetValueOrDefault(method.Name.Replace("Decimal", ""));
        }

        return operation != null && typeSuffix != null ? $"{operation}_{typeSuffix}" : null;
    }

    private Type GetTypeFromSuffix(string suffix) => suffix switch
    {
        "i32" => typeof(int),
        "i64" => typeof(long),
        "f32" => typeof(float),
        "f64" => typeof(double),
        "decimal" => typeof(decimal),
        _ => Thrower.NotSupported<Type>($"Type suffix '{suffix}' is not supported.")
    };

    private class CompilationContext
    {
        public List<Instruction> NewInstructions { get; } = new();
    }
}