using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;
using DynamicMethodCalling;

namespace BasicCilCompiler;

public class DynamicMethodExecutor : IExecutor<DynamicMethod>
{
    private readonly Dictionary<DynamicMethod, DynamicMethodInvoker<object>> _cache = [];
    private DynamicMethodInvoker<object> _q;

    public object Execute(DynamicMethod compilation)
    {
        if (_q == null) _q = new DynamicMethodInvoker<object>(compilation);
        return _q.Invoke();
    }
}