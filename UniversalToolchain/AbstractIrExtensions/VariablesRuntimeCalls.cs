namespace AbstractIrExtensions;

public static class VariablesRuntimeCalls
{
    public static T LoadLocal<T>(VariablesContext context, string name)
    {
        context = context.ArgNotNull();
        return context.LoadLocal<T>(name);
    }

    public static void StoreLocal<T>(T value, string name, VariablesContext context)
    {
        context = context.ArgNotNull();
        context.StoreLocal(name, value);
    }
}
