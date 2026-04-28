namespace AbstractIrExtensions;

public static class AbstractIrExtensions
{
    public static void SetValueToLocal<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, string locName, Type locType)
    {
        locName = locName.ArgNotNull();
        locType = locType.ArgNotNull();

        air.Push(locName);
        air.CallCSharp(VariablesRuntimeMethodDescriptors.LoadVariablesContextDescriptor);
        air.CallCSharp(VariablesRuntimeMethodDescriptors.CreateStoreLocalMethod(locType));
    }

    public static void LdLoc<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, string locName, Type locType)
    {
        locName = locName.ArgNotNull();
        locType = locType.ArgNotNull();

        air.CallCSharp(VariablesRuntimeMethodDescriptors.LoadVariablesContextDescriptor);
        air.Push(locName);
        air.CallCSharp(VariablesRuntimeMethodDescriptors.CreateLoadLocalMethod(locType));
    }

    public static void LdExternal<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, int slot, Type valueType)
    {
        air.Intrinsic("load_external", slot, valueType);
    }

    public static void StExternal<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, int slot, Type valueType)
    {
        air.Intrinsic("store_external", slot, valueType);
    }

}
