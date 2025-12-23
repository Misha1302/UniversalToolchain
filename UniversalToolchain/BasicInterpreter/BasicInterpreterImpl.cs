using System.Reflection.Emit;
using System.Runtime.InteropServices;
using BasicCore.ExecutorWrapper;
using ExceptionsManager;
using GrEmit;

namespace BasicInterpreter;

public class BasicInterpreterImpl : IExecutor<object>
{
    // private readonly Dictionary<string, int> _labels = [];
    // private readonly List<object> _stack = [];
    // private int _ip;
    // private bool[] _isIntrinsic = [];
    // private List<(GroboIL, DynamicMethod)> _methods = [];
    //
    // public object Execute(List<(GroboIL, DynamicMethod)> methods)
    // {
    //     _methods = methods;
    //     Preprocess(methods);
    //     InterpretInternal();
    //     return _stack[0];
    // }
    //
    // private void Preprocess(List<(GroboIL, DynamicMethod)> targetDynamicMethods)
    // {
    //     _isIntrinsic = new bool[targetDynamicMethods.Count];
    //
    //     for (var index = 0; index < targetDynamicMethods.Count; index++)
    //     {
    //         var method = targetDynamicMethods[index];
    //         if (method.Item2.Name.Contains("!Intrinsic"))
    //             _isIntrinsic[index] = true;
    //     }
    //
    //     for (var index = 0; index < targetDynamicMethods.Count; index++)
    //     {
    //         var method = targetDynamicMethods[index];
    //         var dm = method.Item2;
    //         if (!dm.Name.Contains("Label_!Intrinsic")) continue;
    //
    //         var name = ExtractLabelName(dm.Name).NotNull();
    //         _labels[name] = index;
    //     }
    // }
    //
    //
    // private void InterpretInternal()
    // {
    //     for (_ip = 0; _ip < _methods.Count; _ip++)
    //     {
    //         var span = CollectionsMarshal.AsSpan(_stack);
    //
    //         var method = _methods[_ip].Item2;
    //         if (_isIntrinsic[_ip])
    //             ExecuteInternal(method);
    //         else ExecuteBasic(method, span);
    //     }
    // }
    //
    // private void ExecuteBasic(DynamicMethod method, Span<object> span)
    // {
    //     var argsCount = method.GetParameters().Length;
    //     var args = span[..argsCount];
    //     var ans = method.Invoke(this, args.ToArray());
    //
    //     _stack.RemoveRange(0, argsCount);
    //
    //     _stack.Insert(0, ans!);
    // }
    //
    // private void ExecuteInternal(DynamicMethod method)
    // {
    //     if (method.Name.StartsWith("Label_!Intrinsic"))
    //     {
    //         // nothing to do
    //     }
    //     else if (method.Name.StartsWith("Goto_!Intrinsic"))
    //     {
    //         var name = ExtractLabelName(method.Name).NotNull();
    //         _ip = _labels[name];
    //     }
    //     else if (method.Name.StartsWith("CondFGoto_!Intrinsic"))
    //     {
    //         var name = ExtractLabelName(method.Name).NotNull();
    //         Thrower.AssertAlways(_stack[0] is bool);
    //         if (_stack[0] is false)
    //             _ip = _labels[name];
    //         _stack.RemoveAt(0);
    //     }
    //     else
    //     {
    //         Thrower.InvalidOpEx($"Unknown intrinsic {method.Name}");
    //     }
    // }
    //
    //
    // private string? ExtractLabelName(string methodName)
    // {
    //     const string intrinsicMarker = "_!Intrinsic_";
    //     var markerIndex = methodName.IndexOf(intrinsicMarker, StringComparison.Ordinal);
    //     return markerIndex >= 0 ? methodName[(markerIndex + intrinsicMarker.Length)..] : null;
    // }
    public object Execute(object compilation)
    {
        throw new NotImplementedException();
    }
}