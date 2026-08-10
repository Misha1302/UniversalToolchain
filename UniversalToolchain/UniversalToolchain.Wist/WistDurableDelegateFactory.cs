using System.Linq.Expressions;
using System.Reflection;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Wist;

internal static class WistDurableDelegateFactory
{
    public static TDelegate Create<TDelegate>(IWistDurableProgram program)
        where TDelegate : Delegate
    {
        ArgumentNullException.ThrowIfNull(program);
        if (program.TryCreateNativeDelegate(typeof(TDelegate), out var native))
            return (TDelegate)native!;

        var invoke = typeof(TDelegate).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new ArgumentException($"Type '{typeof(TDelegate)}' is not a delegate type.", nameof(TDelegate));
        if (invoke.ReturnType == typeof(void))
            throw new NotSupportedException("Wist compiled delegates must return a value.");

        var parameters = invoke.GetParameters()
            .Select(static parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        var arguments = Expression.NewArrayInit(
            typeof(object),
            parameters.Select(static parameter => Expression.Convert(parameter, typeof(object))));
        var invokeProgram = Expression.Call(
            Expression.Constant(program),
            typeof(IWistDurableProgram).GetMethod(nameof(IWistDurableProgram.Invoke))!,
            arguments);
        var convert = typeof(WistResultConverter)
            .GetMethod(nameof(WistResultConverter.ConvertTo), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .MakeGenericMethod(invoke.ReturnType);
        var body = Expression.Call(convert, invokeProgram);
        return Expression.Lambda<TDelegate>(body, parameters).Compile();
    }
}
