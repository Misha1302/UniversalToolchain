namespace DotnetAirHelper;

public static class AirTypes
{
    private static readonly Dictionary<string, Action<Instruction, List<Type>>> _intrinsicsProcessors = [];

    static AirTypes()
    {
        TryRegisterIntrinsic(
            "call C#",
            (instruction, stack) =>
            {
                var method = instruction.Operands[1].Get<MethodInfo>();

                var methodParams = method.GetParameters().Select(x => x.ParameterType).ToList();
                var stackTypes = stack.TakeLast(methodParams.Count).ToList();
                var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
                if (!method.IsStatic)
                {
                    Thrower.AssertAlways(method.DeclaringType != null);

                    targetTypes.Insert(0, method.DeclaringType);
                    methodParams.Insert(0, method.DeclaringType);
                    stackTypes.Insert(0, method.DeclaringType);
                }
                method = GenericTypeResolver.MakeGenericMethod(method, targetTypes);

                for (var i = 0; i < targetTypes.Count; i++)
                    stack.Pop();
                if (method.ReturnType != typeof(void))
                    stack.Push(method.ReturnType);
            }
        );
        TryRegisterIntrinsic(
            "call C# ctor",
            (instruction, stack) =>
            {
                var ctor = instruction.Operands[1].Get<ConstructorInfo>();
                foreach (var _ in ctor.GetParameters()) stack.Pop();
                stack.Push(ctor.DeclaringType.NotNull());
            }
        );
    }

    public static bool TryRegisterIntrinsic(string name, Action<Instruction, List<Type>> processIntrinsic) => _intrinsicsProcessors.TryAdd(name, processIntrinsic);

    public static void ProcessTypesIntrinsic(Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        if (_intrinsicsProcessors.TryGetValue(name, out var processor))
            processor(instruction, stack);
        else
            Thrower.InvalidOpEx($"Unknown intrinsic {instruction}");
    }
}