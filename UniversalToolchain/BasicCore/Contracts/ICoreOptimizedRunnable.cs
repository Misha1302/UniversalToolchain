namespace BasicCore.Contracts;

public interface ICoreOptimizedRunnable
{
    void PrepareToRun(string code, OrderedDictionary<string, Type>? parameters = null);
    object? RunPrepared();
}