// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using System.Runtime.InteropServices;
using BasicCore.ExecutorWrapper;
using BasicCore.TranslatorWrapper;

namespace BasicInterpreter;

public class BasicInterpreterImpl : IExecutor
{
    private readonly List<DynamicMethod> _methods = [];
    private readonly List<object> _stack = [];
    private int _ip;

    public object Execute(Bytecode bytecode)
    {
        return Interpret(bytecode);
    }

    public object Interpret(Bytecode bytecode)
    {
        Initialize(bytecode);
        InterpretInternal();
        return _stack[^1];
    }

    private void InterpretInternal()
    {
        for (_ip = 0; _ip < _methods.Count; _ip++)
        {
            var span = CollectionsMarshal.AsSpan(_stack);
            var argsCount = _methods[_ip].GetParameters().Length;
            var args = span[^argsCount..];
            var ans = _methods[_ip].Invoke(this, args.ToArray());

            _stack.RemoveRange(_stack.Count - argsCount, argsCount);

            _stack.Add(ans!);
        }
    }

    /// <summary>
    ///     Initialize _methods and _stack
    /// </summary>
    /// <param name="bytecode"></param>
    private void Initialize(Bytecode bytecode)
    {
        var stack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var args = stack.Take(convertable.ParamsCount).ToList();
            var method = convertable.ToDynamicMethod(stack.Count != 0 ? stack[^1] : null, args);
            for (var i = 0; i < convertable.ParamsCount; i++) stack.RemoveAt(stack.Count - 1);
            stack.Add(method.ReturnType);
            _methods.Add(method);
        }

        _stack.Clear();
    }
}