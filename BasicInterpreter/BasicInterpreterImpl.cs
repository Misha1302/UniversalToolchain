// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using System.Runtime.InteropServices;
using BasicCore.ExecutorWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;

namespace BasicInterpreter;

public class BasicInterpreterImpl : IExecutor
{
    private readonly Dictionary<string, int> _labels = [];
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
            var method = _methods[_ip];
            if (method.Name.Contains("!Intrinsic"))
            {
                ExecuteInternal(method);
                continue;
            }

            var argsCount = method.GetParameters().Length;
            var args = span[^argsCount..];
            var ans = method.Invoke(this, args.ToArray());

            _stack.RemoveRange(_stack.Count - argsCount, argsCount);

            _stack.Add(ans!);
        }
    }

    private void ExecuteInternal(DynamicMethod method)
    {
        if (method.Name.Contains("Label_!Intrinsic"))
            _labels[method.Name[(method.Name.LastIndexOf('_') + 1)..]] = _ip;
        else if (method.Name.Contains("Goto_!Intrinsic"))
            _ip = _labels[method.Name[(method.Name.LastIndexOf('_') + 1)..]];
        else Thrower.InvalidOpEx($"Unknown intrinsic {method.Name}");
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
            if (method.ReturnType != typeof(void))
                stack.Add(method.ReturnType);
            _methods.Add(method);
        }

        _stack.Clear();
    }
}