using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;

namespace BasicCilCompiler.Execution;

public class DynamicMethodExecutor : IExecutor<DynamicMethod>
{
    public object Execute(DynamicMethod compilation) => compilation.Invoke(null, null)!;
}