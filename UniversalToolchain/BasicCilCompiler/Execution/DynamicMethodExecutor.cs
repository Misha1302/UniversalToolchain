namespace BasicCilCompiler.Execution;

public class DynamicMethodExecutor : IExecutor<DynamicMethod>
{
    public object Execute(DynamicMethod compilation, IExecutionEnvironment environment)
    {
        var parameters = compilation.GetParameters();
        var values = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            values[i] = environment.GetExternalValue(i);

        return compilation.Invoke(null, values)!;
    }
}