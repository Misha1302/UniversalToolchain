// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using System.Runtime.InteropServices;
using BasicCore.ExecutorWrapper;
using ExceptionsManager;
using GrEmit;

namespace BasicInterpreter;

public class BasicInterpreterImpl : IExecutor
{
    private readonly Dictionary<string, int> _labels = [];
    private readonly List<object> _stack = [];
    private int _ip;
    private List<(GroboIL, DynamicMethod)> _methods = [];

    public object Execute(List<(GroboIL, DynamicMethod)> targetDynamicMethods)
    {
        _methods = targetDynamicMethods;
        InterpretInternal();
        return _stack[^1];
    }

    private void InterpretInternal()
    {
        for (_ip = 0; _ip < _methods.Count; _ip++)
        {
            var span = CollectionsMarshal.AsSpan(_stack);
            var method = _methods[_ip];
            if (method.Item2.Name.Contains("!Intrinsic"))
            {
                ExecuteInternal(method.Item2);
                continue;
            }

            var argsCount = method.Item2.GetParameters().Length;
            var args = span[^argsCount..];
            var ans = method.Item2.Invoke(this, args.ToArray());

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
}