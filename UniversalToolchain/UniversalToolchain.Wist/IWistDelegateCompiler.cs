using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Wist;

internal interface IWistDelegateCompiler
{
    TDelegate CompileDelegate<TDelegate>(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings)
        where TDelegate : Delegate;
}
