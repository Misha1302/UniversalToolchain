using System.Reflection;
using BasicCore.Core;

namespace VariablesModule.Runtime;

public static class VariablesRuntimeMethodDescriptors
{
    private static readonly MethodInfo LoadLocalDefinition = typeof(VariablesRuntimeCalls)
        .GetMethod(nameof(VariablesRuntimeCalls.LoadLocal))
        .NotNull();

    private static readonly MethodInfo StoreLocalDefinition = typeof(VariablesRuntimeCalls)
        .GetMethod(nameof(VariablesRuntimeCalls.StoreLocal))
        .NotNull();

    public static CSharpCallDescriptor LoadVariablesContextDescriptor { get; } = new(
        typeof(VariablesRuntimeCallProvider).GetMethod(nameof(VariablesRuntimeCallProvider.LoadVariablesContext)).NotNull(),
        new CSharpCallReceiver.ExecutionScopedProvider(typeof(VariablesRuntimeCallProvider)));

    public static MethodInfo CreateLoadLocalMethod(Type valueType)
    {
        valueType = valueType.ArgNotNull();
        return LoadLocalDefinition.MakeGenericMethod(valueType);
    }

    public static MethodInfo CreateStoreLocalMethod(Type valueType)
    {
        valueType = valueType.ArgNotNull();
        return StoreLocalDefinition.MakeGenericMethod(valueType);
    }
}