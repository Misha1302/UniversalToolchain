using BasicCore.ExecutorWrapper;

namespace BasicCore;

public interface IMiddleEndCoreModule<TCompilationOutput>
{
    TCompilationOutput ProcessCompilation(TCompilationOutput current)
    {
        return current;
    }

    void InitMethodsCompiler(IAbstractMethodsCompiler<TCompilationOutput> compiler)
    {
    }

    void InitExecutor(IExecutor<TCompilationOutput> executor)
    {
    }
}