namespace UniversalToolchain.Wist;

/// <summary>
/// Wist shipped components are per-engine but do not currently carry a verified reentrancy contract.
/// Reject overlap instead of serializing it invisibly or pretending PerSession implies thread safety.
/// </summary>
internal sealed class WistOperationConcurrencyGate
{
    private int _active;

    public IDisposable Enter()
    {
        if (Interlocked.CompareExchange(ref _active, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "Concurrent operations on one WistEngine instance are not supported. " +
                "Use a separate WistEngine per concurrent operation stream.");
        }

        return new Lease(this);
    }

    private void Exit() => Volatile.Write(ref _active, 0);

    private sealed class Lease(WistOperationConcurrencyGate owner) : IDisposable
    {
        private WistOperationConcurrencyGate? _owner = owner;

        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Exit();
    }
}
