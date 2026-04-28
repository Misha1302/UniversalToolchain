namespace BasicCore.Execution;

public static class ExternalRuntimeCalls
{
    public static T LoadExternal<T>(IExecutionEnvironment environment, int slot)
    {
        environment = environment.ArgNotNull();
        var value = environment.GetExternalValue(slot);

        if (value is T typed)
            return typed;

        return (T)Convert.ChangeType(value.NotNull(), typeof(T));
    }

    public static void StoreExternal<T>(T value, int slot, IExecutionEnvironment environment)
    {
        environment = environment.ArgNotNull();
        environment.SetExternalValue(slot, value);
    }
}
