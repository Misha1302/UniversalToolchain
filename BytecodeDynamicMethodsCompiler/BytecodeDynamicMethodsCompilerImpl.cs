// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using BasicCore;
using BasicCore.TranslatorWrapper;
using GrEmit;

namespace BytecodeDynamicMethodsCompiler;

public class BytecodeDynamicMethodsCompilerImpl : IBytecodeDynamicMethodsCompiler
{
    public List<(GroboIL, DynamicMethod)> Compile(Bytecode bytecode)
    {
        var methods = (List<(GroboIL, DynamicMethod)>)[];
        var stack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var args = stack.Take(convertable.ParamsCount).ToList();
            var method = convertable.ToDynamicMethod(stack.Count != 0 ? stack[^1] : null, args);
            for (var i = 0; i < convertable.ParamsCount; i++) stack.RemoveAt(stack.Count - 1);
            if (method.Item2.ReturnType != typeof(void))
                stack.Add(method.Item2.ReturnType);
            methods.Add(method);
        }

        return methods;
    }
}