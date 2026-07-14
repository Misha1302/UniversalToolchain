using BasicCilCompiler.Execution;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Wist;

internal sealed class WistCilDelegateCompiler : IWistDelegateCompiler
{
    public TDelegate CompileDelegate<TDelegate>(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings)
        where TDelegate : Delegate
    {
        var output = CompileOutput(host, formula, declaredBindings);
        return output.HasConstantPool
            ? (TDelegate)output.Method.CreateDelegate(typeof(TDelegate), output.ConstantPool)
            : (TDelegate)output.Method.CreateDelegate(typeof(TDelegate));
    }

    private static CilCompilationOutput CompileOutput(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings) =>
        host.GetBackendSpecificArtifactCompiler<CilCompilationOutput>(WistBackendAliases.CompilerAlias)
            .Compile(formula, declaredBindings)
            .CompilationOutput;
}
