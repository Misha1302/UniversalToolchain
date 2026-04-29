namespace DynamicMethodCalling;

public static class CompiledArtifactNativePointerExtensions
{
    public static ExecutionBoundNativePointer2<TArg1, TArg2, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        Thrower.AssertAlways(artifact.DeclaredBindings.Count >= 2, "Dynamic method artifact must declare at least two bindings.");
        var arg1Slot = artifact.SlotsByName[artifact.DeclaredBindings[0].Name];
        var arg2Slot = artifact.SlotsByName[artifact.DeclaredBindings[1].Name];

        return new ExecutionBoundNativePointer2<TArg1, TArg2, TResult>(artifact, environment, arg1Slot, arg2Slot);
    }
}
