using BasicCore.Execution;
using BasicCore.ExecutorWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceExecutor : IExecutor<DialectDefinitionSlice>
{
    public object? Execute(DialectDefinitionSlice compilation, IExecutionEnvironment environment)
    {
        compilation = compilation.ArgNotNull();

        return compilation;
    }
}