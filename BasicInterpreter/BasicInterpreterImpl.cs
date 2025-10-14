// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;

namespace BasicInterpreter;

public class BasicInterpreterImpl(InterpreterConfiguration configuration)
{
    private readonly List<DynamicMethod> _methods = [];
    private readonly List<object> _stack = [];
    private int _ip;

    public object Interpret()
    {
        Optimize();
        InterpretInternal();
        return _stack[^1];
    }

    private void InterpretInternal()
    {
        for (_ip = 0; _ip < _methods.Count; _ip++)
        {
            var args = _stack[^_methods[_ip].GetParameters().Length..].ToArray();
            var ans = _methods[_ip].Invoke(this, args);
            _stack.Add(ans!);
        }
    }

    private void Optimize()
    {
        var stack = new List<Type>();
        foreach (var instruction in configuration.Bytecode.Instructions)
        foreach (var op in instruction.Ops)
        {
            var method = op.Value.ToDynamicMethod(stack.Count != 0 ? stack[^1] : null, stack);
            _methods.Add(method);
        }
    }
}