using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using SettableGettableModule;
using SettableGettableModule.Contracts;
using SettableGettableModule.Core;

namespace AbstractIrExtensions;

public static class AbstractIrExtensions
{
    public static void LdLocRef<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, string locName, Type locType)
    {
        air.Push(locName);
        air.ActWithLoc(
            locType,
            nameof(VariablesContainer<>.GetRef)
        );
    }

    public static void StLocByRef<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, Type locType)
    {
        var varRef = typeof(VariableReference<>).MakeGenericType(locType);
        var method = varRef.GetMethod(nameof(VariableReference<>.SetValue)).NotNull();
        air.CallCSharp(method);
    }

    public static void SetValueToLocal<TIdentifier>(this IGenericAbstractIR<TIdentifier> air, string locName, Type locType)
    {
        air.LdLocRef(locName, locType);
        air.SetValueToSettable(locType);
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
        air.Push(locName);
        air.ActWithLoc(
            locType,
            nameof(VariablesContainer<>.Get)
        );
    }

    private static void ActWithLoc<TIdentifier>(
        this IGenericAbstractIR<TIdentifier> air,
        Type locType,
        string methodName
    )
    {
        var variablesContainer = typeof(VariablesContainer<>).MakeGenericType(locType);
        var method = variablesContainer.GetMethod(methodName).NotNull();
        air.CallCSharp(method);
    }

    private static class VariablesHelper
    {
        public static void SetValueTo<T, TSettable>(T value, TSettable settable) where TSettable : ISettable<T>
        {
            settable.SetValue(value);
        }
    }
}