using BasicCore.ExecutorWrapper;

namespace BasicCore;

public interface IMiddleEndCoreModule<TCompilationOutput>
{
    TCompilationOutput ProcessCompilation(TCompilationOutput current) => current;

    void InitExecutor(IExecutor<TCompilationOutput> executor)
    {
    }

    void InitMethodsCompiler(IAbstractIrCompiler<TCompilationOutput> compiler)
    {
    }
}