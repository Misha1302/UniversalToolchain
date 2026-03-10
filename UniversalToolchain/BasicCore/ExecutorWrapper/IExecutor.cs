namespace BasicCore.ExecutorWrapper;

public interface IExecutor<in TCompilationOutput>
{
    object? Execute(TCompilationOutput compilation, IExecutionEnvironment environment);
}