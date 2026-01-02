using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;
using DynamicMethodCalling;

namespace BasicCilCompiler;

public class DynamicMethodExecutor : IExecutor<DynamicMethod>
{
    private readonly Dictionary<DynamicMethod, DynamicMethodInvoker<Func<object>>> _cache = [];

    public object? Execute(DynamicMethod compilation)
    {
        if (!_cache.TryGetValue(compilation, out var del))
            _cache.Add(compilation, del = new DynamicMethodInvoker<Func<object>>(compilation));
        return del.Invoke();
    }
}