namespace BasicCilCompiler.Execution;

public class DynamicMethodExecutor : IExecutor<DynamicMethod>
{
    public object Execute(DynamicMethod compilation, IExecutionEnvironment environment)
    {
        var parameters = compilation.GetParameters();
        var hasEnvironmentArgument = parameters.Length > 0 && parameters[0].ParameterType == typeof(IExecutionEnvironment);
        var values = new object?[parameters.Length];
        var externalSlotOffset = hasEnvironmentArgument ? 1 : 0;

        if (hasEnvironmentArgument)
            values[0] = environment;

        for (var i = externalSlotOffset; i < parameters.Length; i++)
            values[i] = environment.GetExternalValue(i - externalSlotOffset);

        return compilation.Invoke(null, values)!;
    }
}