namespace BasicCore;

public interface ICoreRunnable
{
    public object? Run(string code, Dictionary<string, object> args);
}