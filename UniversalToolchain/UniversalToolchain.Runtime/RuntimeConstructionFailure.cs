using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace UniversalToolchain.Runtime;

internal static class RuntimeConstructionFailure
{
    [DoesNotReturn]
    public static void Rethrow(
        Exception primaryException,
        IReadOnlyList<Exception> cleanupExceptions,
        string aggregateMessage)
    {
        ArgumentNullException.ThrowIfNull(primaryException);
        ArgumentNullException.ThrowIfNull(cleanupExceptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateMessage);

        if (cleanupExceptions.Count == 0)
            ExceptionDispatchInfo.Capture(primaryException).Throw();

        var combined = new List<Exception>(cleanupExceptions.Count + 1)
        {
            primaryException
        };
        combined.AddRange(cleanupExceptions);
        throw new AggregateException(aggregateMessage, combined);
    }

    public static IReadOnlyList<Exception> DisposeSynchronouslyCollect(params object?[] owners)
    {
        ArgumentNullException.ThrowIfNull(owners);
        List<Exception>? errors = null;
        for (var index = owners.Length - 1; index >= 0; index--)
        {
            try
            {
                switch (owners[index])
                {
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                    case IAsyncDisposable asyncDisposable:
                        asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                        break;
                }
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
        }

        return errors ?? [];
    }

    public static async ValueTask<IReadOnlyList<Exception>> DisposeAsynchronouslyCollect(params object?[] owners)
    {
        ArgumentNullException.ThrowIfNull(owners);
        List<Exception>? errors = null;
        for (var index = owners.Length - 1; index >= 0; index--)
        {
            try
            {
                switch (owners[index])
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
        }

        return errors ?? [];
    }
}
