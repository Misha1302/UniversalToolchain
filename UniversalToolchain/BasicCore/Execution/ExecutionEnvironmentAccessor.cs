namespace BasicCore.Execution;

public static class ExecutionEnvironmentAccessor
{
    public static object? GetExternalValue(IExecutionEnvironment environment, int slot) => environment.GetExternalValue(slot);

    public static void SetExternalValue(IExecutionEnvironment environment, int slot, object? value) =>
        environment.SetExternalValue(slot, value);
}
