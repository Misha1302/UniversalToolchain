namespace DynamicMethodCalling;

public sealed class ExecutionBoundNativePointer2<TArg1, TArg2, TResult>
{
    private readonly IExecutionEnvironment _environment;
    private readonly RuntimeEnvironmentAdapter _adapter;
    private readonly DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TResult> _invoker;
    private readonly int _arg1Slot;
    private readonly int _arg2Slot;

    public ExecutionBoundNativePointer2(ICompiledArtifact<DynamicMethod> artifact, IExecutionEnvironment environment, int arg1Slot, int arg2Slot)
    {
        artifact = artifact.ArgNotNull();
        environment = environment.ArgNotNull();

        _environment = environment;
        _adapter = new RuntimeEnvironmentAdapter(_environment);
        _invoker = new DynamicMethodInvoker<IExecutionEnvironment, TArg1, TArg2, TResult>(artifact.CompilationOutput);
        _arg1Slot = arg1Slot;
        _arg2Slot = arg2Slot;
    }

    public TResult Invoke(TArg1 arg1, TArg2 arg2)
    {
        _adapter.SetCurrentArguments(_arg1Slot, _arg2Slot, arg1, arg2);
        return _invoker.Invoke(_adapter, arg1, arg2);
    }

    private sealed class RuntimeEnvironmentAdapter(IExecutionEnvironment innerEnvironment) : IExecutionEnvironment
    {
        private TArg1? _arg1;
        private TArg2? _arg2;
        private int _arg1Slot;
        private int _arg2Slot;

        public object? GetExternalValue(int slot)
        {
            return slot switch
            {
                _ when slot == _arg1Slot => _arg1,
                _ when slot == _arg2Slot => _arg2,
                _ => innerEnvironment.GetExternalValue(slot)
            };
        }

        public void SetExternalValue(int slot, object? value) => innerEnvironment.SetExternalValue(slot, value);

        public TContext GetOrCreate<TContext>(RuntimeContextKey key, Func<TContext> factory) where TContext : class =>
            innerEnvironment.GetOrCreate(key, factory);

        public object GetRequiredProvider(Type providerType) => innerEnvironment.GetRequiredProvider(providerType);

        public void SetCurrentArguments(int arg1Slot, int arg2Slot, TArg1 arg1, TArg2 arg2)
        {
            _arg1Slot = arg1Slot;
            _arg2Slot = arg2Slot;
            _arg1 = arg1;
            _arg2 = arg2;
        }
    }
}
