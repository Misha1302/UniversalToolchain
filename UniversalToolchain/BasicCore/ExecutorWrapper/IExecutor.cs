namespace BasicCore.ExecutorWrapper;

public interface IExecutor<TCompilationOutput>
{
    object? Execute(TCompilationOutput compilation);
}