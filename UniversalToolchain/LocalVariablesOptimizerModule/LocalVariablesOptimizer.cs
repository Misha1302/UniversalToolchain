using UniversalToolchain.Dialects.Abstractions;
namespace LocalVariablesOptimizerModule;

[AutoRegisterService]
[DialectOptimizerAlias("LocalVariablesOptimization")]
[DialectRuntimeExport("Optimizer", "LocalVariablesOptimization")]
public class LocalVariablesOptimizer : IIRProcessingModule
{
    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        _ = current.ArgNotNull();
        _ = compiler.ArgNotNull();

        // TODO: Future local-variable optimization must operate on the C# runtime call graph
        // produced by VariablesRuntimeCalls, not by introducing local-variable intrinsics.
        return current;
    }
}
