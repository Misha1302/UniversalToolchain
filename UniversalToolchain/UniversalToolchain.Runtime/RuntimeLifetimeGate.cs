namespace UniversalToolchain.Runtime;

/// <summary>
/// Coordinates synchronous runtime operations with deterministic one-time disposal.
/// New operations are rejected once disposal starts; external disposal waits for in-flight operations.
/// Disposal from a call context that owns an active lease fails immediately instead of self-deadlocking.
/// </summary>
internal sealed class RuntimeLifetimeGate
{
    private static readonly AsyncLocal<LeaseScope?> CurrentLease = new();

    private readonly object _gate = new();
    private int _activeOperations;
    private LifetimeState _state = LifetimeState.Running;

    public IDisposable EnterOperation(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        lock (_gate)
        {
            if (_state != LifetimeState.Running)
                throw new ObjectDisposedException(owner.GetType().FullName);
            _activeOperations++;
        }

        var scope = new LeaseScope(this, CurrentLease.Value);
        CurrentLease.Value = scope;
        return new OperationLease(this, scope);
    }

    /// <summary>Returns true only to the caller that owns disposal.</summary>
    public bool BeginDispose()
    {
        if (OwnsLeaseInCurrentContext())
        {
            throw new InvalidOperationException(
                "A runtime cannot be disposed from an execution context that currently owns one of its active operation leases.");
        }

        lock (_gate)
        {
            if (_state == LifetimeState.Disposed)
                return false;
            if (_state == LifetimeState.Disposing)
            {
                while (_state != LifetimeState.Disposed)
                    Monitor.Wait(_gate);
                return false;
            }

            _state = LifetimeState.Disposing;
            while (_activeOperations != 0)
                Monitor.Wait(_gate);
            return true;
        }
    }

    public void CompleteDispose()
    {
        lock (_gate)
        {
            _state = LifetimeState.Disposed;
            Monitor.PulseAll(_gate);
        }
    }

    private bool OwnsLeaseInCurrentContext()
    {
        for (var scope = CurrentLease.Value; scope != null; scope = scope.Previous)
        {
            if (scope.IsActive && ReferenceEquals(scope.Owner, this))
                return true;
        }
        return false;
    }

    private void ExitOperation(LeaseScope scope)
    {
        RemoveCurrentScope(scope);
        scope.Deactivate();
        lock (_gate)
        {
            if (_activeOperations <= 0)
                throw new InvalidOperationException("Runtime operation lease accounting underflowed.");
            _activeOperations--;
            if (_activeOperations == 0)
                Monitor.PulseAll(_gate);
        }
    }

    private static void RemoveCurrentScope(LeaseScope scope)
    {
        if (ReferenceEquals(CurrentLease.Value, scope))
        {
            CurrentLease.Value = scope.Previous;
            return;
        }

        // Operation leases are expected to be disposed in LIFO order. Failing closed keeps an
        // out-of-order lease from silently corrupting ownership tracking and disposal safety.
        throw new InvalidOperationException("Runtime operation leases must be disposed in reverse acquisition order.");
    }

    private sealed class OperationLease(RuntimeLifetimeGate owner, LeaseScope scope) : IDisposable
    {
        private RuntimeLifetimeGate? _owner = owner;

        public void Dispose()
        {
            var current = Interlocked.Exchange(ref _owner, null);
            current?.ExitOperation(scope);
        }
    }

    private sealed class LeaseScope(RuntimeLifetimeGate owner, LeaseScope? previous)
    {
        private int _active = 1;

        public RuntimeLifetimeGate Owner { get; } = owner;
        public LeaseScope? Previous { get; } = previous;
        public bool IsActive => Volatile.Read(ref _active) != 0;

        public void Deactivate() => Interlocked.Exchange(ref _active, 0);
    }

    private enum LifetimeState
    {
        Running,
        Disposing,
        Disposed
    }
}
