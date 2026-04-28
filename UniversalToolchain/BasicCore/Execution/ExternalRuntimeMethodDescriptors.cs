using System.Reflection;
using BasicCore.Core;

namespace BasicCore.Execution;

public static class ExternalRuntimeMethodDescriptors
{
    private static readonly MethodInfo LoadExternalDefinition = typeof(ExternalRuntimeCalls)
        .GetMethod(nameof(ExternalRuntimeCalls.LoadExternal))
        .NotNull();

    private static readonly MethodInfo StoreExternalDefinition = typeof(ExternalRuntimeCalls)
        .GetMethod(nameof(ExternalRuntimeCalls.StoreExternal))
        .NotNull();

    public static CSharpCallDescriptor LoadEnvironmentDescriptor { get; } = new(
        typeof(ExternalRuntimeCallProvider).GetMethod(nameof(ExternalRuntimeCallProvider.LoadEnvironment)).NotNull(),
        new CSharpCallReceiver.ExecutionScopedProvider(typeof(ExternalRuntimeCallProvider)));

    public static MethodInfo CreateLoadExternalMethod(Type valueType)
    {
        valueType = valueType.ArgNotNull();
        return LoadExternalDefinition.MakeGenericMethod(valueType);
    }

    public static MethodInfo CreateStoreExternalMethod(Type valueType)
    {
        valueType = valueType.ArgNotNull();
        return StoreExternalDefinition.MakeGenericMethod(valueType);
    }
}
