using BasicCore.Execution;
using BasicCore.ExecutorWrapper;

namespace BasicCore.Core;

internal sealed class PreparedExecution<TCompilationOutput>(
    string sourceText,
    TCompilationOutput compilationOutput,
    IExecutor<TCompilationOutput> executor,
    IExecutionEnvironment executionEnvironment)
{
    public string SourceText { get; } = sourceText;

    public TCompilationOutput CompilationOutput { get; } = compilationOutput;

    public IExecutor<TCompilationOutput> Executor { get; } = executor;

    public IExecutionEnvironment ExecutionEnvironment { get; } = executionEnvironment;
}