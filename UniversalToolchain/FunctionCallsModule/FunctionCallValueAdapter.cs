namespace FunctionCallsModule;

public static class FunctionCallValueAdapter
{
    public static TValue GetValue<TValue>(IGettable<TValue> value)
    {
        value = value.ArgNotNull();
        return value.GetValue();
    }
}
