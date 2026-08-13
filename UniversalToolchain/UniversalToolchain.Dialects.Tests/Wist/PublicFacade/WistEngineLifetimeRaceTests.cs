using System.Collections.Concurrent;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistEngineLifetimeRaceTests
{
    [Test]
    public async Task DisposeRacingEvaluate_NeverExposesTornRuntimePublication()
    {
        const int iterations = 32;
        const int workers = 8;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            using var start = new ManualResetEventSlim();
            var engine = WistEngine.CreateRestrictedArithmetic();
            var values = new ConcurrentBag<double>();
            var exceptions = new ConcurrentBag<Exception>();

            var evaluations = Enumerable.Range(0, workers)
                .Select(_ => Task.Run(() =>
                {
                    start.Wait();
                    try
                    {
                        values.Add(engine.Evaluate<double>("1 + 2"));
                    }
                    catch (Exception exception)
                    {
                        exceptions.Add(exception);
                    }
                }))
                .ToArray();
            var dispose = Task.Run(() =>
            {
                start.Wait();
                engine.Dispose();
            });

            start.Set();
            await Task.WhenAll(evaluations.Append(dispose)).WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Multiple(() =>
            {
                Assert.That(values, Is.All.EqualTo(3.0d), $"iteration={iteration}");
                Assert.That(
                    exceptions.All(static exception =>
                        exception is ObjectDisposedException ||
                        exception is InvalidOperationException invalid &&
                        invalid.Message.Contains("Concurrent operations on one WistEngine instance are not supported", StringComparison.Ordinal)),
                    Is.True,
                    $"iteration={iteration}; dispose may reject operations and overlapping operations fail fast, but no torn runtime state is observable.");
            });

            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.Evaluate<double>("1 + 2"));
        }
    }
}
