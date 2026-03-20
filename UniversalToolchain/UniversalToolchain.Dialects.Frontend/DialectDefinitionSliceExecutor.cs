namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceExecutor : IExecutor<DialectDefinitionSlice>
{
    public object? Execute(DialectDefinitionSlice compilation, IExecutionEnvironment environment)
    {
        if (compilation == null)
            Thrower.ArgumentNull(nameof(compilation));

        return compilation;
    }
}