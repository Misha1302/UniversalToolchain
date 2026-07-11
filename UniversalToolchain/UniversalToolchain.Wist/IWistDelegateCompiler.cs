using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Wist;

internal interface IWistDelegateCompiler
{
    TDelegate CompileDelegate<TDelegate>(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings)
        where TDelegate : Delegate;

    WistFunc<TArg0, TResult> CompileFunc<TArg0, TResult>(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings);

    WistFunc<TArg0, TArg1, TResult> CompileFunc<TArg0, TArg1, TResult>(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings);

    WistFunc<TArg0, TArg1, TArg2, TResult> CompileFunc<TArg0, TArg1, TArg2, TResult>(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings);
}
