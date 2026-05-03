namespace DynamicMethodCalling;

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

    public TResult Invoke() => _invoker.Invoke(_adapter);
}

public sealed class ExecutionBoundNativePointer<TArg1, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1)
    {
        _arg1.Value = arg1;
        return _invoker.Invoke(_adapter, arg1);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        return _invoker.Invoke(_adapter, arg1, arg2);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();
    private readonly ExecutionBoundArgument<TArg3> _arg3 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2, _arg3]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        _arg3.Value = arg3;
        return _invoker.Invoke(_adapter, arg1, arg2, arg3);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();
    private readonly ExecutionBoundArgument<TArg3> _arg3 = new();
    private readonly ExecutionBoundArgument<TArg4> _arg4 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2, _arg3, _arg4]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        _arg3.Value = arg3;
        _arg4.Value = arg4;
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();
    private readonly ExecutionBoundArgument<TArg3> _arg3 = new();
    private readonly ExecutionBoundArgument<TArg4> _arg4 = new();
    private readonly ExecutionBoundArgument<TArg5> _arg5 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2, _arg3, _arg4, _arg5]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        _arg3.Value = arg3;
        _arg4.Value = arg4;
        _arg5.Value = arg5;
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();
    private readonly ExecutionBoundArgument<TArg3> _arg3 = new();
    private readonly ExecutionBoundArgument<TArg4> _arg4 = new();
    private readonly ExecutionBoundArgument<TArg5> _arg5 = new();
    private readonly ExecutionBoundArgument<TArg6> _arg6 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2, _arg3, _arg4, _arg5, _arg6]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        _arg3.Value = arg3;
        _arg4.Value = arg4;
        _arg5.Value = arg5;
        _arg6.Value = arg6;
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();
    private readonly ExecutionBoundArgument<TArg3> _arg3 = new();
    private readonly ExecutionBoundArgument<TArg4> _arg4 = new();
    private readonly ExecutionBoundArgument<TArg5> _arg5 = new();
    private readonly ExecutionBoundArgument<TArg6> _arg6 = new();
    private readonly ExecutionBoundArgument<TArg7> _arg7 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        _arg3.Value = arg3;
        _arg4.Value = arg4;
        _arg5.Value = arg5;
        _arg6.Value = arg6;
        _arg7.Value = arg7;
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();
    private readonly ExecutionBoundArgument<TArg3> _arg3 = new();
    private readonly ExecutionBoundArgument<TArg4> _arg4 = new();
    private readonly ExecutionBoundArgument<TArg5> _arg5 = new();
    private readonly ExecutionBoundArgument<TArg6> _arg6 = new();
    private readonly ExecutionBoundArgument<TArg7> _arg7 = new();
    private readonly ExecutionBoundArgument<TArg8> _arg8 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7, _arg8]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        _arg3.Value = arg3;
        _arg4.Value = arg4;
        _arg5.Value = arg5;
        _arg6.Value = arg6;
        _arg7.Value = arg7;
        _arg8.Value = arg8;
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();
    private readonly ExecutionBoundArgument<TArg3> _arg3 = new();
    private readonly ExecutionBoundArgument<TArg4> _arg4 = new();
    private readonly ExecutionBoundArgument<TArg5> _arg5 = new();
    private readonly ExecutionBoundArgument<TArg6> _arg6 = new();
    private readonly ExecutionBoundArgument<TArg7> _arg7 = new();
    private readonly ExecutionBoundArgument<TArg8> _arg8 = new();
    private readonly ExecutionBoundArgument<TArg9> _arg9 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7, _arg8, _arg9]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        _arg3.Value = arg3;
        _arg4.Value = arg4;
        _arg5.Value = arg5;
        _arg6.Value = arg6;
        _arg7.Value = arg7;
        _arg8.Value = arg8;
        _arg9.Value = arg9;
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
}

public sealed class ExecutionBoundNativePointer<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>
{
    private readonly ExecutionBoundNativePointerEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> _invoker;
    private readonly ExecutionBoundArgument<TArg1> _arg1 = new();
    private readonly ExecutionBoundArgument<TArg2> _arg2 = new();
    private readonly ExecutionBoundArgument<TArg3> _arg3 = new();
    private readonly ExecutionBoundArgument<TArg4> _arg4 = new();
    private readonly ExecutionBoundArgument<TArg5> _arg5 = new();
    private readonly ExecutionBoundArgument<TArg6> _arg6 = new();
    private readonly ExecutionBoundArgument<TArg7> _arg7 = new();
    private readonly ExecutionBoundArgument<TArg8> _arg8 = new();
    private readonly ExecutionBoundArgument<TArg9> _arg9 = new();
    private readonly ExecutionBoundArgument<TArg10> _arg10 = new();

    internal ExecutionBoundNativePointer(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();
        _adapter = new ExecutionBoundNativePointerEnvironmentAdapter(environment, [_arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7, _arg8, _arg9, _arg10]);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(artifact.CompilationOutput);
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10)
    {
        _arg1.Value = arg1;
        _arg2.Value = arg2;
        _arg3.Value = arg3;
        _arg4.Value = arg4;
        _arg5.Value = arg5;
        _arg6.Value = arg6;
        _arg7.Value = arg7;
        _arg8.Value = arg8;
        _arg9.Value = arg9;
        _arg10.Value = arg10;
        return _invoker.Invoke(_adapter, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }
}
