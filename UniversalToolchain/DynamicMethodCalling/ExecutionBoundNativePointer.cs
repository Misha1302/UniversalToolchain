namespace DynamicMethodCalling;

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 0);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke()
    {
        return _invoker.Invoke(_adapter);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 1);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1)
    {
        _adapter.SetCurrentArgument(0, arg1);
        return _invoker.Invoke(_adapter, arg1);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 2);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        return _invoker.Invoke(_adapter, arg1, arg2);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 3);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        _adapter.SetCurrentArgument(2, arg3);
        return _invoker.Invoke(_adapter, arg1, arg2, arg3);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 4);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        _adapter.SetCurrentArgument(2, arg3);
        _adapter.SetCurrentArgument(3, arg4);
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 5);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        _adapter.SetCurrentArgument(2, arg3);
        _adapter.SetCurrentArgument(3, arg4);
        _adapter.SetCurrentArgument(4, arg5);
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 6);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        _adapter.SetCurrentArgument(2, arg3);
        _adapter.SetCurrentArgument(3, arg4);
        _adapter.SetCurrentArgument(4, arg5);
        _adapter.SetCurrentArgument(5, arg6);
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 7);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        _adapter.SetCurrentArgument(2, arg3);
        _adapter.SetCurrentArgument(3, arg4);
        _adapter.SetCurrentArgument(4, arg5);
        _adapter.SetCurrentArgument(5, arg6);
        _adapter.SetCurrentArgument(6, arg7);
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 8);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        _adapter.SetCurrentArgument(2, arg3);
        _adapter.SetCurrentArgument(3, arg4);
        _adapter.SetCurrentArgument(4, arg5);
        _adapter.SetCurrentArgument(5, arg6);
        _adapter.SetCurrentArgument(6, arg7);
        _adapter.SetCurrentArgument(7, arg8);
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 9);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        _adapter.SetCurrentArgument(2, arg3);
        _adapter.SetCurrentArgument(3, arg4);
        _adapter.SetCurrentArgument(4, arg5);
        _adapter.SetCurrentArgument(5, arg6);
        _adapter.SetCurrentArgument(6, arg7);
        _adapter.SetCurrentArgument(7, arg8);
        _adapter.SetCurrentArgument(8, arg9);
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
}

/// <summary>
///     Wraps a DynamicMethod native pointer whose hidden first argument is an execution environment.
/// </summary>
/// <remarks>
///     Instances are invocation-session objects and are not safe for concurrent Invoke calls.
///     Create a separate wrapper per concurrent execution flow.
/// </remarks>
public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> _invoker;

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, 10);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(artifact.CompilationOutput);
    }

    /// <summary>
///     Invokes the native pointer while supplying the bound execution environment internally.
/// </summary>
    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10)
    {
        _adapter.SetCurrentArgument(0, arg1);
        _adapter.SetCurrentArgument(1, arg2);
        _adapter.SetCurrentArgument(2, arg3);
        _adapter.SetCurrentArgument(3, arg4);
        _adapter.SetCurrentArgument(4, arg5);
        _adapter.SetCurrentArgument(5, arg6);
        _adapter.SetCurrentArgument(6, arg7);
        _adapter.SetCurrentArgument(7, arg8);
        _adapter.SetCurrentArgument(8, arg9);
        _adapter.SetCurrentArgument(9, arg10);
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }
}
