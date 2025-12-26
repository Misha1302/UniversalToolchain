using System.Reflection;
using BytecodeDynamicMethodsCompiler;
using DotnetHelper;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using ListExtensions;

namespace DotnetAirHelper;

public static class AirTypes
{
    public static void ProcessTypesIntrinsic(Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        if (name == "call C#")
        {
            var method = instruction.Operands[1].Get<MethodInfo>();
            Thrower.AssertAlways(method.DeclaringType != null);

            var methodParams = method.GetParameters().Select(x => x.ParameterType).ToList();
            var stackTypes = stack.TakeLast(methodParams.Count).Reverse().ToList();
            var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
            if (!method.IsStatic)
            {
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
        else if (name == "call C# ctor")
        {
            var ctor = instruction.Operands[1].Get<ConstructorInfo>();
            foreach (var _ in ctor.GetParameters()) stack.Pop();
            stack.Push(ctor.DeclaringType.NotNull());
        }
        else
        {
            Thrower.InvalidOpEx($"Unknown intrinsic {instruction}");
        }
    }
}