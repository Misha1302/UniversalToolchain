using System.Reflection;
using BasicCore.Attributes;
using BasicCore.Contracts;
using ConditionsModule.Visitors;
using DotnetAirHelper;
using IntermediateRepresentationAbstractions;
using JetBrains.Annotations;
using ListExtensions;
using ObjectExtensions;
using UniversalIntermediateRepresentation;

namespace ConditionsModule.Optimizers;

[AutoRegisterService]
[UsedImplicitly]
public class BooleanOptimizerModule : IIRProcessingModule
{
    private static readonly IReadOnlyList<string> _standardModuleIntrinsics =
    [
        "boolean_and", "boolean_or", "boolean_not"
    ];

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (_standardModuleIntrinsics.Any(x => !compiler.SupportedIntrinsics.Contains(x)))
            return current;

        InitializeAirTypes();
        return OptimizeNativeLoads(current);
    }

    private void InitializeAirTypes()
    {
        AirTypes.TryRegisterIntrinsic(
            "boolean_and",
            (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(bool));
            }
        );
        AirTypes.TryRegisterIntrinsic(
            "boolean_or",
            (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(bool));
            }
        );
        AirTypes.TryRegisterIntrinsic(
            "boolean_not",
            (_, stack) =>
            {
                stack.Pop();
                stack.Push(typeof(bool));
            }
        );
    }

    private IAbstractIR OptimizeNativeLoads(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var context = new CompilationContext();

        var methodToIntrinsic = new Dictionary<string, string>
        {
            [nameof(BooleanVisitor.BooleanOperations.And)] = "boolean_and",
            [nameof(BooleanVisitor.BooleanOperations.Or)] = "boolean_or",
            [nameof(BooleanVisitor.BooleanOperations.Not)] = "boolean_not"
        };

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            if (instruction.UOpCode == UOpCode.Intrinsic)
                if (instruction.Operands.Count >= 2 && instruction.Operands[0] == "call C#")
                {
                    var m = instruction.Operands[1].Get<MethodInfo>();

                    if (m.DeclaringType == typeof(BooleanVisitor.BooleanOperations))
                        if (methodToIntrinsic.TryGetValue(m.Name, out var intrinsicName))
                        {
                            context.NewInstructions.Add(new Instruction(UOpCode.Intrinsic, [intrinsicName]));
                            continue;
                        }
                }

            context.NewInstructions.Add(instruction);
        }

        var result = new AbstractIR();
        result.AppendInstructions(context.NewInstructions);
        return result;
    }

    private class CompilationContext
    {
        public List<Instruction> NewInstructions { get; } =
        [
        ];
    }
}