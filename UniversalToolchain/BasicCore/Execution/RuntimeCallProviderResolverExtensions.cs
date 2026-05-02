namespace BasicCore.Execution;

public static class RuntimeCallProviderResolverExtensions
{
    public static object GetRequiredProvider(IExecutionEnvironment environment, Type providerType)
    {
        environment = environment.ArgNotNull();
        providerType = providerType.ArgNotNull();

        return environment.GetRequiredProvider(providerType);
    }
}