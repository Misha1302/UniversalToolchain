// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection;
using GrEmit;

namespace IlCodeGeneratorFactory;

public static class IlCodeGenerator
{
    public static void Throw<T>(this GroboIL il, string exceptionMessage) where T : Exception
    {
        il.Ldstr(exceptionMessage);
        il.Newobj(typeof(T).GetConstructor([typeof(string)]));
        il.Throw();
    }

    public static void IntrinsicNotImplemented(this GroboIL il)
    {
        il.Throw<NotImplementedException>("Intrinsic function was not overloaded");
    }

    public static void LdArgsAndCall(this GroboIL il, MethodInfo call, Func<int, Type> ldArg)
    {
        var parameters = call.GetParameters();
        var argTypes = new List<Type>();
        for (var i = 0; i < parameters.Length + (call.IsStatic ? 0 : 1); i++)
        {
            var sourceType = ldArg(i);
            argTypes.Add(sourceType);

            if (!call.IsStatic && i == parameters.Length) continue;

            var targetType = parameters[i].ParameterType.ContainsGenericParameters
                ? MakeGenericType(parameters[i].ParameterType, ((IEnumerable<Type>)argTypes).Reverse().ToList())
                : parameters[i].ParameterType;

            if (sourceType.IsAssignableTo(targetType) && sourceType != targetType && sourceType.IsValueType)
            {
                il.Box(sourceType);
                il.Castclass(targetType);
            }
            else if (sourceType.IsAssignableTo(targetType) && sourceType != targetType && !sourceType.IsValueType)
            {
                il.Castclass(targetType);
            }
        }

        il.Call(MakeGenericMethod(call, argTypes));
    }

    private static MethodInfo MakeGenericMethod(MethodInfo call, List<Type> argTypes)
    {
        if (!call.ContainsGenericParameters) return call;

        var genericTypes = call.GetGenericArguments()
            .Select((x, i) => x.FullName == null ? argTypes[i] : x)
            .ToArray();

        return call.GetGenericMethodDefinition().MakeGenericMethod(genericTypes);
    }

    private static Type MakeGenericType(Type parameterType, List<Type> sourceTypes)
    {
        var gArgs = parameterType.GetGenericArguments();
        if (!parameterType.IsGenericType)
            return sourceTypes[0];

        var genericTypes = gArgs
            .Select((x, i) => x.FullName == null ? sourceTypes[i] : x)
            .ToArray();

        return parameterType.GetGenericTypeDefinition().MakeGenericType(genericTypes);
    }
}