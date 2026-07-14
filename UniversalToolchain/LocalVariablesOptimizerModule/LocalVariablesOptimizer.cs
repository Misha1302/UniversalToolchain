namespace LocalVariablesOptimizerModule;

public class LocalVariablesOptimizer : IAirOptimizer
{
    public IAbstractIR Optimize(IAbstractIR current)
    {
        _ = current.ArgNotNull();

        // TODO: Future local-variable optimization must operate on the C# runtime call graph
        // produced by VariablesRuntimeCalls, not by introducing local-variable intrinsics.
        return current;
    }
}