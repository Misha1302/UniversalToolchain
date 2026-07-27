using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Wist;

internal interface IWistDelegateCompiler
{
    TDelegate CompileDelegate<TDelegate>(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings,
        string backend,
        WistRuntimeBoundary runtimeBoundary)
        where TDelegate : Delegate;
}
