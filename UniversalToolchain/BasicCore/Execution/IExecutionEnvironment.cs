namespace BasicCore.Execution;

public interface IExecutionEnvironment : IRuntimeContextStore, IRuntimeCallProviderResolver
{
    object? GetExternalValue(int slot);

    void SetExternalValue(int slot, object? value);
}