using BasicCore.Capabilities;
using BasicCore.Builtins;
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
        methodBody = methodBody.ArgNotNull();
        air.Intrinsic(IntrinsicCapabilityIds.CallCSharp, methodBody);
    }

    public static void CallCSharp<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, CSharpCallDescriptor descriptor)
    {
        descriptor = descriptor.ArgNotNull();
        air.Intrinsic(IntrinsicCapabilityIds.CallCSharp, descriptor);
    }

    public static void CallCSharp<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, ConstructorInfo ctor)
    {
        ctor = ctor.ArgNotNull();
        air.Intrinsic(IntrinsicCapabilityIds.CallCSharpConstructor, ctor);
    }

    public static void Rotate<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, params Type[] types)
    {
        var instructionOffset = air.Instructions.Count;
        var locals = types
            .Select((type, index) => $"__rotate_{instructionOffset:D8}_{index:D4}_{type.FullName}")
            .ToArray();
        for (var i = 0; i < locals.Length; i++)
            air.SetValueToLocal(locals[i], types[i]);
        for (var i = 0; i < locals.Length; i++)
            air.LdLoc(locals[i], types[i]);
    }
}