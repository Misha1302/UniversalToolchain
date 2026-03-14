namespace AbstractIrExtensions;

public static class GenericAbstractIrExtensions
{
    public static void LoadLabel<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, TIdentifier labelId)
    {
        air.Push(labelId!);
    }

    public static void SetLabel<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, TIdentifier labelId)
    {
        air.LoadLabel(labelId);
        air.SetLabel(labelId);
    }

    public static void CallCSharp<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, MethodInfo methodBody)
    {
        air.Intrinsic("call C#", methodBody);
    }

    public static void CallCSharp<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, ConstructorInfo ctor)
    {
        air.Intrinsic("call C# ctor", ctor);
    }

    public static void Rotate<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, params Type[] types)
    {
        var locals = types.Select(_ => Guid.NewGuid().ToString()).ToArray();
        for (var i = 0; i < locals.Length; i++)
            air.SetValueToLocal(locals[i], types[i]);
        for (var i = 0; i < locals.Length; i++)
            air.LdLoc(locals[i], types[i]);
    }
}