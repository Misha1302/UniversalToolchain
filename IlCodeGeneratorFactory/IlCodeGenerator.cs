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
        for (var i = 0; i < parameters.Length; i++)
        {
            var type = ldArg(i);
            if (parameters[i].ParameterType == typeof(object) && type.IsValueType)
                il.Box(type);
        }

        il.Call(call);
    }
}