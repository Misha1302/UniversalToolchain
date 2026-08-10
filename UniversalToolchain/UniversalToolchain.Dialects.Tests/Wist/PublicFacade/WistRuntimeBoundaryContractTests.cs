using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
[NonParallelizable]
public sealed class WistRuntimeBoundaryContractTests
{
    private static readonly string[] NumberPresets =
    [
        "full-default",
        "function-calls-safe-math"
    ];

    [TestCase("full-default", "cil")]
    [TestCase("full-default", "interpreter")]
    [TestCase("minimal-arithmetic", "interpreter")]
    [TestCase("function-calls-safe-math", "cil")]
    [TestCase("full-default-native", "cil")]
    public void ObjectResults_AreNormalizedToStableClrTypes(string presetId, string backendId)
    {
        using var engine = Create(presetId, backendId);

        var evaluated = engine.Evaluate<object>("2 + 3");
        var compiled = engine.Compile<Func<object>>("2 + 3").CompiledDelegate();
        var evaluatedContract = engine.Evaluate<IConvertible>("2 + 3");
        var compiledContract = engine.Compile<Func<IConvertible>>("2 + 3").CompiledDelegate();
        var expectsNormalizedDouble = !string.Equals(presetId, "full-default-native", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(evaluated, Is.InstanceOf<IConvertible>());
            Assert.That(compiled, Is.InstanceOf<IConvertible>());
            Assert.That(Convert.ToDouble(evaluated), Is.EqualTo(5d));
            Assert.That(Convert.ToDouble(compiled), Is.EqualTo(5d));
            Assert.That(Convert.ToDouble(evaluatedContract), Is.EqualTo(5d));
            Assert.That(Convert.ToDouble(compiledContract), Is.EqualTo(5d));
            Assert.That(evaluated.GetType().Assembly.GetName().Name, Is.Not.EqualTo("NumbersModule"));
            Assert.That(compiled.GetType().Assembly.GetName().Name, Is.Not.EqualTo("NumbersModule"));
            Assert.That(evaluatedContract.GetType().Assembly.GetName().Name, Is.Not.EqualTo("NumbersModule"));
            Assert.That(compiledContract.GetType().Assembly.GetName().Name, Is.Not.EqualTo("NumbersModule"));
            Assert.That(AssemblyLoadContext.GetLoadContext(evaluated.GetType().Assembly)?.IsCollectible, Is.False);
            Assert.That(AssemblyLoadContext.GetLoadContext(compiled.GetType().Assembly)?.IsCollectible, Is.False);
            Assert.That(AssemblyLoadContext.GetLoadContext(evaluatedContract.GetType().Assembly)?.IsCollectible, Is.False);
            Assert.That(AssemblyLoadContext.GetLoadContext(compiledContract.GetType().Assembly)?.IsCollectible, Is.False);
            if (expectsNormalizedDouble)
            {
                Assert.That(evaluated.GetType(), Is.EqualTo(typeof(double)));
                Assert.That(compiled.GetType(), Is.EqualTo(typeof(double)));
                Assert.That(evaluatedContract.GetType(), Is.EqualTo(typeof(double)));
                Assert.That(compiledContract.GetType(), Is.EqualTo(typeof(double)));
            }
        });
    }

    [TestCaseSource(nameof(NumberPresetBackendCases))]
    public void ClrDoubleParameters_WorkAcrossAllPublicOperations(string presetId, string backendId)
    {
        using var engine = Create(presetId, backendId);

        var evaluated = engine.Evaluate<double>("x + 3.0", new { x = 2.0d });
        var validation = engine.Validate("x + 3.0", new { x = 2.0d });
        var compiled = engine.Compile<Func<double, double>>("x + 3.0", "x");
        var attempt = engine.TryCompile<Func<double, double>>("x + 3.0", "x");

        Assert.Multiple(() =>
        {
            Assert.That(evaluated, Is.EqualTo(5d));
            Assert.That(validation.IsValid, Is.True, string.Join(Environment.NewLine, validation.Diagnostics.Select(static d => d.Message)));
            Assert.That(compiled.CompiledDelegate(2d), Is.EqualTo(5d));
            Assert.That(attempt.IsSuccess, Is.True, string.Join(Environment.NewLine, attempt.Diagnostics.Select(static d => d.Message)));
            Assert.That(attempt.Program!.CompiledDelegate(2d), Is.EqualTo(5d));
        });
    }

    [Test]
    public void Dispose_ReleasesRuntimeContextEvenWhenDisposedEngineRemainsReachable()
    {
        var (engine, runtime) = CreateDisposedEngine("full-default");
        try
        {
            CollectUntilDead(runtime);
            Assert.That(runtime.IsAlive, Is.False);
            GC.KeepAlive(engine);
        }
        finally
        {
            engine.Dispose();
        }
    }

    [TestCase("minimal-arithmetic")]
    [TestCase("minimal-arithmetic-grouped")]
    [TestCase("full-default")]
    [TestCase("function-calls-safe-math")]
    [TestCase("composition-restricted")]
    public void Dispose_AfterOneShotEvaluation_ReleasesCollectibleRuntimeContext(string presetId)
    {
        var runtime = CreateAndDisposeEngine(presetId);

        CollectUntilDead(runtime);

        Assert.That(runtime.IsAlive, Is.False, $"Canonical runtime for preset '{presetId}' remained rooted after disposal.");
    }

    private static IEnumerable<TestCaseData> NumberPresetBackendCases()
    {
        foreach (var presetId in NumberPresets)
        {
            yield return new TestCaseData(presetId, "cil");
            yield return new TestCaseData(presetId, "interpreter");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WistEngine Engine, WeakReference Runtime) CreateDisposedEngine(string presetId)
    {
        var engine = WistEngine.Create(WistEngineOptions.FromPresetId(presetId));
        Assert.That(engine.Evaluate<double>("2 + 3"), Is.EqualTo(5d));
        var runtime = CaptureOwnedRuntime(engine);
        engine.Dispose();
        return (engine, runtime);
    }

    [TestCase("cil")]
    [TestCase("interpreter")]
    public void FullDefault_TrustedClrInterop_AdaptsNumbersInBothDirections(string backend)
    {
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("full-default"),
            BackendId = backend,
            AllowedAssemblies = [typeof(Math).Assembly]
        });

        var evaluated = engine.Evaluate<double>("System.Math.Sqrt(16.0) + 1.0");
        var compiled = engine.Compile<Func<double>>("System.Math.Sqrt(16.0) + 1.0");

        Assert.Multiple(() =>
        {
            Assert.That(evaluated, Is.EqualTo(5.0d).Within(1e-9));
            Assert.That(compiled.CompiledDelegate(), Is.EqualTo(5.0d).Within(1e-9));
        });
    }

    [Test]
    public void Dispose_ReleasesAllSequentialRuntimeContexts()
    {
        var entries = Enumerable.Range(0, 8)
            .Select(static _ => CreateDisposedEngine("full-default"))
            .ToArray();

        try
        {
            for (var attempt = 0; attempt < 80 && entries.Any(static entry => entry.Runtime.IsAlive); attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Thread.Sleep(10);
            }

            Assert.That(entries.Count(static entry => entry.Runtime.IsAlive), Is.Zero);
            GC.KeepAlive(entries);
        }
        finally
        {
            foreach (var entry in entries)
                entry.Engine.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeEngine(string presetId)
    {
        WeakReference runtime;
        using (var engine = WistEngine.Create(WistEngineOptions.FromPresetId(presetId)))
        {
            Assert.That(engine.Evaluate<double>("2 + 3"), Is.EqualTo(5d));
            runtime = CaptureOwnedRuntime(engine);
        }
        return runtime;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureOwnedRuntime(WistEngine engine)
    {
        var runtimeField = typeof(WistEngine).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(runtimeField, Is.Not.Null, "WistEngine must own one canonical runtime field during S09.");
        var runtime = runtimeField!.GetValue(engine);
        Assert.That(runtime, Is.Not.Null);
        return new WeakReference(runtime!, trackResurrection: false);
    }

    private static void CollectUntilDead(WeakReference reference)
    {
        for (var attempt = 0; attempt < 80 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(10);
        }
    }

    private static WistEngine Create(string presetId, string backendId) =>
        WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset(presetId),
            BackendId = backendId
        });
}
