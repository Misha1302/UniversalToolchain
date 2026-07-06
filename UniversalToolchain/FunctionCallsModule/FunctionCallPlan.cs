namespace FunctionCallsModule;

public sealed class FunctionCallPlan
{
    public FunctionCallPlan(
        BuiltinFunctionRuntimeBinding binding,
        IReadOnlyList<MethodInfo?> argumentAdapters,
        MethodInfo? resultAdapterFactory,
        ConstructorInfo? resultAdapterConstructor,
        int adapterCount)
    {
        Binding = binding.ArgNotNull();
        ArgumentAdapters = argumentAdapters.ArgNotNull();
        ResultAdapterFactory = resultAdapterFactory;
        ResultAdapterConstructor = resultAdapterConstructor;
        AdapterCount = adapterCount;
    }

    public BuiltinFunctionRuntimeBinding Binding { get; }

    public IReadOnlyList<MethodInfo?> ArgumentAdapters { get; }

    public MethodInfo? ResultAdapterFactory { get; }

    public ConstructorInfo? ResultAdapterConstructor { get; }

    public int AdapterCount { get; }
}
