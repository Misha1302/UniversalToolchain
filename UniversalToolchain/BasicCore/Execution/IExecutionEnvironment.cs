namespace BasicCore.Execution;

public interface IExecutionEnvironment
{
    object? GetExternalValue(int slot);

    void SetExternalValue(int slot, object? value);
}
