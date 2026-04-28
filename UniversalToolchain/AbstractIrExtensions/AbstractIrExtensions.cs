namespace AbstractIrExtensions;

public static class AbstractIrExtensions
{
    public static void SetValueToLocal<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, string locName, Type locType)
    {
        locName = locName.ArgNotNull();
        locType = locType.ArgNotNull();

        air.Push(locName);
        air.CallCSharp(_loadVariablesContextDescriptor);
        air.CallCSharp(GetOrCreateStoreLocalMethod(locType));
    }

    public static void SetValueToSettable<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, Type type)
    {
        var method = typeof(VariablesHelper)
            .GetMethod(nameof(VariablesHelper.SetValueTo))
            .NotNull()
            .MakeGenericMethod(type, typeof(VariableReference<>).MakeGenericType(type));
        air.CallCSharp(method);
    }

    public static void LdLoc<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, string locName, Type locType)
    {
        locName = locName.ArgNotNull();
        locType = locType.ArgNotNull();

        air.CallCSharp(_loadVariablesContextDescriptor);
        air.Push(locName);
        air.CallCSharp(GetOrCreateLoadLocalMethod(locType));
    }

    public static void LdExternal<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, int slot, Type valueType)
    {
        air.Intrinsic("load_external", slot, valueType);
    }

    public static void StExternal<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, int slot, Type valueType)
    {
        air.Intrinsic("store_external", slot, valueType);
    }

    private static class VariablesHelper
    {
        public static void SetValueTo<T, TSettable>(T value, TSettable settable) where TSettable : ISettable<T>
        {
            settable.SetValue(value);
        }
    }

    private static readonly CSharpCallDescriptor _loadVariablesContextDescriptor = new(
        typeof(VariablesRuntimeCallProvider).GetMethod(nameof(VariablesRuntimeCallProvider.LoadVariablesContext)).NotNull(),
        new CSharpCallReceiver.ExecutionScopedProvider(typeof(VariablesRuntimeCallProvider)));

    private static readonly MethodInfo _loadLocalDefinition = typeof(VariablesRuntimeCalls)
        .GetMethod(nameof(VariablesRuntimeCalls.LoadLocal))
        .NotNull();

    private static readonly MethodInfo _storeLocalDefinition = typeof(VariablesRuntimeCalls)
        .GetMethod(nameof(VariablesRuntimeCalls.StoreLocal))
        .NotNull();

    private static readonly Dictionary<Type, MethodInfo> _loadLocalMethods = [];
    private static readonly Dictionary<Type, MethodInfo> _storeLocalMethods = [];

    private static MethodInfo GetOrCreateLoadLocalMethod(Type type)
    {
        if (_loadLocalMethods.TryGetValue(type, out var method))
            return method;

        method = _loadLocalDefinition.MakeGenericMethod(type);
        _loadLocalMethods[type] = method;
        return method;
    }

    private static MethodInfo GetOrCreateStoreLocalMethod(Type type)
    {
        if (_storeLocalMethods.TryGetValue(type, out var method))
            return method;

        method = _storeLocalDefinition.MakeGenericMethod(type);
        _storeLocalMethods[type] = method;
        return method;
    }
}
