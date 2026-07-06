using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wistc;

internal static class WistCliTraceWriter
{
    public const string SchemaVersion = "wist-debug-trace/2";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static void WriteSuccess(
        string path,
        string code,
        string dialect,
        string backend,
        object? result,
        WistCliTraceOptions? options = null)
    {
        options ??= new WistCliTraceOptions();

        var document = CreateDocument(
            code,
            dialect,
            backend,
            new WistCliTraceResult("success", Sanitize(result?.GetType().FullName, options), null, null),
            [
                CreateStage(options, WistCliTraceStageCatalog.Input, WistCliTraceStageStatus.Success, ("sourcePolicy", "redacted")),
                CreateStage(options, WistCliTraceStageCatalog.DialectComposition, WistCliTraceStageStatus.Success, ("dialect", dialect)),
                CreateStage(options, WistCliTraceStageCatalog.RuntimeSelection, WistCliTraceStageStatus.Success, ("backend", backend)),
                CreateStage(options, WistCliTraceStageCatalog.BytecodeTranslation, WistCliTraceStageStatus.Success, ("detail", "summary-only")),
                CreateStage(options, WistCliTraceStageCatalog.BackendArtifact, WistCliTraceStageStatus.Success, ("detail", "redacted")),
                CreateStage(options, WistCliTraceStageCatalog.BackendExecution, WistCliTraceStageStatus.Success, ("resultType", result?.GetType().FullName ?? "null"))
            ],
            options);

        Write(path, document);
    }

    public static void WriteFailure(
        string path,
        string code,
        string dialect,
        string backend,
        Exception exception,
        WistCliTraceOptions? options = null)
    {
        exception = exception.ArgNotNull();
        options ??= new WistCliTraceOptions();

        var document = CreateDocument(
            code,
            dialect,
            backend,
            new WistCliTraceResult("failed", null, Sanitize(exception.GetType().FullName, options), Sanitize(exception.Message, options)),
            [
                CreateStage(options, WistCliTraceStageCatalog.Input, WistCliTraceStageStatus.Success, ("sourcePolicy", "redacted")),
                CreateStage(options, WistCliTraceStageCatalog.DialectComposition, WistCliTraceStageStatus.Success, ("dialect", dialect)),
                CreateStage(options, WistCliTraceStageCatalog.RuntimeSelection, WistCliTraceStageStatus.Success, ("backend", backend)),
                CreateStage(options, WistCliTraceStageCatalog.BytecodeTranslation, WistCliTraceStageStatus.Failed, ("detail", "summary-only")),
                CreateStage(options, WistCliTraceStageCatalog.BackendArtifact, WistCliTraceStageStatus.Skipped, ("detail", "redacted")),
                CreateStage(options, WistCliTraceStageCatalog.BackendExecution, WistCliTraceStageStatus.Skipped, ("errorType", exception.GetType().FullName ?? exception.GetType().Name))
            ],
            options);

        Write(path, document);
    }

    private static WistCliTraceDocument CreateDocument(
        string code,
        string dialect,
        string backend,
        WistCliTraceResult result,
        IReadOnlyList<WistCliTraceStage> stages,
        WistCliTraceOptions options)
    {
        code = code.ArgNotNull();
        options = options.ArgNotNull();

        return new WistCliTraceDocument(
            SchemaVersion,
            options.CreatedAtUtc ?? DateTimeOffset.UtcNow,
            new WistCliTraceMetadata(
                "Wistc",
                Sanitize(dialect, options).NotNull(),
                Sanitize(backend, options).NotNull(),
                true,
                true),
            new WistCliSourceSummary(code.Length, ComputeSha256(code)),
            stages,
            result);
    }

    private static WistCliTraceStage CreateStage(
        WistCliTraceOptions options,
        WistCliTraceStageDescriptor descriptor,
        string status,
        params (string Key, string? Value)[] metadata)
    {
        descriptor = descriptor.ArgNotNull();
        var metadataMap = metadata
            .Where(static item => item.Value != null)
            .ToDictionary(static item => item.Key, item => Sanitize(item.Value, options)!, StringComparer.Ordinal);

        return new WistCliTraceStage(descriptor.Id, descriptor.Kind, descriptor.Owner, status, metadataMap);
    }

    private static void Write(string path, WistCliTraceDocument document)
    {
        if (string.IsNullOrWhiteSpace(path))
            Thrower.Argument(nameof(path), "Trace path must not be empty.");

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(document, _jsonOptions);
        File.WriteAllText(path, json + Environment.NewLine, Encoding.UTF8);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? Sanitize(string? value, WistCliTraceOptions options)
    {
        if (value == null)
            return null;

        if (options.MaxMetadataValueLength <= 0)
            Thrower.Argument(nameof(options), "Trace metadata value length must be positive.");

        if (value.Length <= options.MaxMetadataValueLength)
            return value;

        return value[..options.MaxMetadataValueLength] + "...";
    }
}
