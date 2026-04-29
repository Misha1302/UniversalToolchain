namespace DynamicMethodCalling;

/// <summary>
///     Creates execution-bound native pointer wrappers for DynamicMethod compiled artifacts.
/// </summary>
public static class CompiledArtifactNativePointerExtensions
{
    /// <summary>
    ///     Creates a two-argument execution-bound native pointer wrapper.
    /// </summary>
    public static ExecutionBoundNativePointer2<TArg1, TArg2, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        Thrower.AssertAlways(
            artifact.DeclaredBindings.Count == 2,
            "Execution-bound native pointer with two arguments requires exactly two declared bindings.");

        var firstBinding = artifact.DeclaredBindings[0];
        var secondBinding = artifact.DeclaredBindings[1];

        Thrower.AssertAlways(
            firstBinding.Type == typeof(TArg1),
            $"Declared binding '{firstBinding.Name}' must have type {typeof(TArg1)} but it has {firstBinding.Type}.");

        Thrower.AssertAlways(
            secondBinding.Type == typeof(TArg2),
            $"Declared binding '{secondBinding.Name}' must have type {typeof(TArg2)} but it has {secondBinding.Type}.");

        if (!artifact.SlotsByName.TryGetValue(firstBinding.Name, out var arg1Slot))
            Thrower.InvalidOpEx($"Declared binding '{firstBinding.Name}' has no slot.");

        if (!artifact.SlotsByName.TryGetValue(secondBinding.Name, out var arg2Slot))
            Thrower.InvalidOpEx($"Declared binding '{secondBinding.Name}' has no slot.");

        Thrower.AssertAlways(arg1Slot == 0, $"Declared binding '{firstBinding.Name}' must use slot 0 but it uses slot {arg1Slot}.");
        Thrower.AssertAlways(arg2Slot == 1, $"Declared binding '{secondBinding.Name}' must use slot 1 but it uses slot {arg2Slot}.");

        return new ExecutionBoundNativePointer2<TArg1, TArg2, TResult>(artifact, environment, arg1Slot, arg2Slot);
    }
}
