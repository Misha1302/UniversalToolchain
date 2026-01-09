namespace BasicCore;

public interface ICoreOptimizedRunnable
{
    void PrepareToRun(string code, Dictionary<string, Type>? parameters = null);
    object? RunPrepared();
}