namespace BasicCilCompiler.Execution;

public class DynamicMethodExecutor : IExecutor<DynamicMethod>, IExecutor<CilCompilationOutput>
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

    public object Execute(CilCompilationOutput compilation, IExecutionEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        var method = compilation.Method;
        var parameters = method.GetParameters();
        var values = new object?[parameters.Length];
        var argumentOffset = 0;

        if (parameters.Length > argumentOffset && parameters[argumentOffset].ParameterType == typeof(ArtifactConstantPool))
        {
            values[argumentOffset] = compilation.ConstantPool
                                     ?? throw new InvalidOperationException("CIL output is missing the required artifact constant pool.");
            argumentOffset++;
        }

        if (parameters.Length > argumentOffset && parameters[argumentOffset].ParameterType == typeof(IExecutionEnvironment))
        {
            values[argumentOffset] = environment;
            argumentOffset++;
        }

        for (var i = argumentOffset; i < parameters.Length; i++)
            values[i] = environment.GetExternalValue(i - argumentOffset);

        return method.Invoke(null, values)!;
    }
}
