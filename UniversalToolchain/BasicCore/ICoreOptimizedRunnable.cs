namespace BasicCore;

public interface ICoreOptimizedRunnable
{
    void PrepareToRun(string code);
    object? RunPrepared();
}