using Wistc;
using System.Text.Json;
using BasicCore.Contracts;

namespace UniversalToolchain.Dialects.Tests.Wist;

public sealed class WistCliDefaultAndListingTests
{
    [Test]
    public void WistCliCustomizationRequest_FromOptions_DoesNotRequestRawDialectTextMutation()
    {
        var request = WistCliCustomizationRequest.FromOptions(new CommonOptions());

        Assert.That(request.HasCustomization, Is.False);
    }

    [Test]
    public void RuntimeListing_UsesRuntimeComponentCatalog()
    {
        var output = WistCliRuntimeListingFormatter.Format(new StaticCatalog(
            [Entry(RuntimeComponentKind.FrontendModule, "Arithmetic", [], "frontend.arithmetic", "ArithmeticModule")],
            [],
            [Entry(RuntimeComponentKind.Backend, "cil", ["compiler"], "backend.cil", "UniversalToolchain.Dialects.Wist")]));

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Modules:"));
            Assert.That(output, Does.Contain("Arithmetic | id: frontend.arithmetic | assembly: ArithmeticModule"));
            Assert.That(output, Does.Contain("cil | aliases: compiler | id: backend.cil | assembly: UniversalToolchain.Dialects.Wist"));
        });
    }

    [Test]
    public void RuntimeListing_Output_IsDeterministicallyOrdered()
    {
        var output = WistCliRuntimeListingFormatter.Format(new StaticCatalog(
            [
                Entry(RuntimeComponentKind.FrontendModule, "Alpha", [], "frontend.alpha", "AlphaModule"),
                Entry(RuntimeComponentKind.FrontendModule, "Beta", [], "frontend.beta", "BetaModule")
            ],
            [],
            []));

        Assert.That(output.IndexOf("  Alpha", StringComparison.Ordinal), Is.LessThan(output.IndexOf("  Beta", StringComparison.Ordinal)));
    }

    [Test]
    public void Parser_Help_DoesNotSelectDefaultRepl()
    {
        var result = WistCliParser.Parse(["--help"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            var message = result.Errors.Single().Message;
            Assert.That(message, Does.StartWith("Usage: wistc <command>"));
            Assert.That(message, Does.Contain("repl"));
            Assert.That(result.Options, Is.Null);
        });
    }

    [Test]
    public void Parser_UnknownOption_DoesNotFallThroughToDefaultRepl()
    {
        var result = WistCliParser.Parse(["--definitely-unknown"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors.Single().Message, Is.EqualTo("Unknown option '--definitely-unknown'."));
            Assert.That(result.Options, Is.Null);
        });
    }

    [Test]
    public void Repl_Run_ExitsOnEndOfInput()
    {
        var originalIn = Console.In;
        var originalOut = Console.Out;
        using var input = new StringReader(string.Empty);
        using var output = new StringWriter();

        try
        {
            Console.SetIn(input);
            Console.SetOut(output);

            var exitCode = new Repl(new ThrowingCoreRunnable()).Run();

            Assert.Multiple(() =>
            {
                Assert.That(exitCode, Is.EqualTo(0));
                Assert.That(output.ToString(), Is.EqualTo("> "));
            });
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void TraceWriter_WriteSuccess_RedactsSourceAndRuntimeValues()
    {
        var tracePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"trace-{Guid.NewGuid():N}.json");
        const string source = "secret_price + 10";

        WistCliTraceWriter.WriteSuccess(tracePath, source, "TraceDialect", "interpreter", 15);

        using var document = JsonDocument.Parse(File.ReadAllText(tracePath));
        var root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("schemaVersion").GetString(), Is.EqualTo(WistCliTraceWriter.SchemaVersion));
            Assert.That(root.GetProperty("metadata").GetProperty("dialect").GetString(), Is.EqualTo("TraceDialect"));
            Assert.That(root.GetProperty("metadata").GetProperty("sourceRedacted").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("metadata").GetProperty("runtimeValuesRedacted").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("source").GetProperty("length").GetInt32(), Is.EqualTo(source.Length));
            Assert.That(root.GetProperty("source").TryGetProperty("text", out _), Is.False);
            Assert.That(root.ToString(), Does.Not.Contain("secret_price"));
            Assert.That(root.GetProperty("stages").GetArrayLength(), Is.GreaterThanOrEqualTo(6));
            Assert.That(root.GetProperty("result").GetProperty("status").GetString(), Is.EqualTo("success"));
        });
    }

    [Test]
    public void TraceWriter_WriteFailure_UsesDeterministicTimestampAndTruncatesMetadata()
    {
        var tracePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"trace-{Guid.NewGuid():N}.json");
        var timestamp = new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
        var exception = new InvalidOperationException(new string('x', 40));

        WistCliTraceWriter.WriteFailure(
            tracePath,
            "secret_value",
            "TraceDialect",
            "interpreter",
            exception,
            new WistCliTraceOptions(timestamp, 12));

        using var document = JsonDocument.Parse(File.ReadAllText(tracePath));
        var root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("createdAtUtc").GetString(), Does.StartWith("2026-07-05T12:00:00"));
            Assert.That(root.GetProperty("result").GetProperty("errorMessage").GetString(), Is.EqualTo("xxxxxxxxxxxx..."));
            Assert.That(root.ToString(), Does.Not.Contain("secret_value"));
            Assert.That(
                root.GetProperty("stages").EnumerateArray().Select(static x => x.GetProperty("id").GetString()),
                Is.EqualTo(WistCliTraceStageCatalog.OrderedStages.Select(static x => x.Id)));
            Assert.That(
                root.GetProperty("stages").EnumerateArray().Select(static x => x.GetProperty("status").GetString()),
                Is.EqualTo(new[]
                {
                    WistCliTraceStageStatus.Success,
                    WistCliTraceStageStatus.Success,
                    WistCliTraceStageStatus.Success,
                    WistCliTraceStageStatus.Failed,
                    WistCliTraceStageStatus.Skipped,
                    WistCliTraceStageStatus.Skipped
                }));
        });
    }

    private static RuntimeComponentManifestEntry Entry(
        RuntimeComponentKind kind,
        string canonicalAlias,
        IReadOnlyList<string> aliases,
        string componentId,
        string assemblySimpleName)
        => new(kind, canonicalAlias, aliases, new RuntimeComponentId(componentId), assemblySimpleName);

    private sealed class StaticCatalog(
        IReadOnlyList<RuntimeComponentManifestEntry> modules,
        IReadOnlyList<RuntimeComponentManifestEntry> optimizers,
        IReadOnlyList<RuntimeComponentManifestEntry> backends) : IRuntimeComponentCatalog
    {
        public bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry)
            => TryResolve(modules, alias, out entry);

        public bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry)
            => TryResolve(optimizers, alias, out entry);

        public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry)
            => TryResolve(backends, alias, out entry);

        public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => modules;

        public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => optimizers;

        public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => backends;

        private static bool TryResolve(
            IReadOnlyList<RuntimeComponentManifestEntry> entries,
            string alias,
            out RuntimeComponentManifestEntry? entry)
        {
            entry = entries.FirstOrDefault(x => x.AllAliases.Contains(alias, StringComparer.Ordinal));
            return entry != null;
        }
    }

    private sealed class ThrowingCoreRunnable : ICoreRunnable
    {
        public object? Run(string code, Dictionary<string, object>? args = null)
        {
            throw new InvalidOperationException("REPL should not execute code when input is already at EOF.");
        }
    }
}
