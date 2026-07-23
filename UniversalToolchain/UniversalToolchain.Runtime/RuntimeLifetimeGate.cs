namespace UniversalToolchain.Runtime;

/// <summary>
/// Coordinates synchronous runtime operations with deterministic one-time disposal.
/// New operations are rejected once disposal starts; disposal waits for in-flight operations.
/// </summary>
internal sealed class RuntimeLifetimeGate
{
    private readonly object _gate = new();
    private int _activeOperations;
    private bool _disposing;
    private bool _disposed;

    public IDisposable EnterOperation(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        lock (_gate)
        {
            if (_disposing || _disposed)
                throw new ObjectDisposedException(owner.GetType().FullName);
            _activeOperations++;
        }
        return new OperationLease(this);
    }

    /// <summary>Returns true only to the caller that owns disposal.</summary>
    public bool BeginDispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return false;
            if (_disposing)
            {
                while (!_disposed)
                    Monitor.Wait(_gate);
                return false;
            }

            _disposing = true;
            while (_activeOperations != 0)
                Monitor.Wait(_gate);
            return true;
        }
    }

    public void CompleteDispose()
    {
        lock (_gate)
        {
            _disposed = true;
            Monitor.PulseAll(_gate);
        }
    }

    private void ExitOperation()
    {
        lock (_gate)
        {
            _activeOperations--;
            if (_activeOperations == 0)
                Monitor.PulseAll(_gate);
        }
    }

    private sealed class OperationLease(RuntimeLifetimeGate owner) : IDisposable
    {
        private RuntimeLifetimeGate? _owner = owner;

        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.ExitOperation();
    }
}
