using System.Reflection;

namespace UniversalIntermediateRepresentation;

public static class GenericAbstractIrExtensions
{
    public static void LoadLabel<TIdentifier>(this GenericAbstractIR<TIdentifier> air, TIdentifier labelId)
    {
        air.Push(Value.Create(labelId));
    }

    public static void SetLabel<TIdentifier>(this GenericAbstractIR<TIdentifier> air, TIdentifier labelId)
    {
        air.LoadLabel(labelId);
        air.SetLabel(labelId);
    }

    public static void Jmp<TIdentifier>(this GenericAbstractIR<TIdentifier> air, TIdentifier labelId)
    {
        air.LoadLabel(labelId);
        air.Jmp();
    }

    public static void JmpIf<TIdentifier>(this GenericAbstractIR<TIdentifier> air, TIdentifier labelId)
    {
        air.LoadLabel(labelId);
        air.JmpIf();
    }

    public static void JmpIfNot<TIdentifier>(this GenericAbstractIR<TIdentifier> air, TIdentifier labelId)
    {
        air.LoadLabel(labelId);
        air.JmpIfNot();
    }

    public static void CallCSharp<TIdentifier>(this GenericAbstractIR<TIdentifier> air, MethodInfo methodBody)
    {
        air.Intrinsic(Value.Create("call C#"), Value.Create(methodBody));
    }

    public static void CallCSharp<TIdentifier>(this GenericAbstractIR<TIdentifier> air, ConstructorInfo ctor)
    {
        air.Intrinsic(Value.Create("call C# ctor"), Value.Create(ctor));
    }

    public static void Rotate(this GenericAbstractIR<Guid> air, int count)
    {
        var locals = Enumerable.Range(0, count).Select(i => Guid.NewGuid()).ToArray();
        foreach (var local in locals)
        {
            air.StLoc(local);
        }
        foreach (var local in locals)
        {
            air.LdLoc(local);
        }
    }
}