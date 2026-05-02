namespace BasicCilCompiler.Execution;

public class DynamicMethodExecutor : IExecutor<DynamicMethod>
{
    public object Execute(DynamicMethod compilation, IExecutionEnvironment environment)
    {
        var parameters = compilation.GetParameters();
        var values = new object?[parameters.Length];
        values[0] = environment;
        for (var i = 1; i < parameters.Length; i++)
            values[i] = environment.GetExternalValue(i - 1);

        return compilation.Invoke(null, values)!;
    }
}