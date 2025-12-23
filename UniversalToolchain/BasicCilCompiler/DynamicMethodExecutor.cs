// ./BasicCilCompiler/BasicCilCompilerImpl.cs

using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;

namespace BasicCilCompiler;

public class DynamicMethodExecutor : IExecutor<DynamicMethod>
{
    public object Execute(DynamicMethod compilation)
    {
        return compilation.Invoke(null, null)!;
    }
}