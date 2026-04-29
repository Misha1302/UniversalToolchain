namespace DynamicMethodCalling;

/// <summary>
///     Creates execution-bound native pointer wrappers for DynamicMethod compiled artifacts.
/// </summary>
public static class CompiledArtifactNativePointerExtensions
{
    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TResult> CreateExecutionBoundNativePointer<TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            []);

        return new ExecutionBoundNativePointer<TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TResult> CreateExecutionBoundNativePointer<TArg1, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1)]);

        return new ExecutionBoundNativePointer<TArg1, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TArg3, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2), typeof(TArg3)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4), typeof(TArg5)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4), typeof(TArg5), typeof(TArg6)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4), typeof(TArg5), typeof(TArg6), typeof(TArg7)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4), typeof(TArg5), typeof(TArg6), typeof(TArg7), typeof(TArg8)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4), typeof(TArg5), typeof(TArg6), typeof(TArg7), typeof(TArg8), typeof(TArg9)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(artifact, environment);
    }

    /// <summary>
///     Creates an execution-bound native pointer wrapper.
/// </summary>
    public static ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> CreateExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(
        this ICompiledArtifact<DynamicMethod> artifact,
        IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        ExecutionBoundNativePointerValidation.ValidateDeclaredBindings(
            artifact,
            [typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4), typeof(TArg5), typeof(TArg6), typeof(TArg7), typeof(TArg8), typeof(TArg9), typeof(TArg10)]);

        return new ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(artifact, environment);
    }

}