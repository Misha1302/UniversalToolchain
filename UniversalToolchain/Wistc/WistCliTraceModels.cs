namespace Wistc;

internal sealed record WistCliTraceDocument(
    string SchemaVersion,
    DateTimeOffset CreatedAtUtc,
    WistCliTraceMetadata Metadata,
    WistCliSourceSummary Source,
    IReadOnlyList<WistCliTraceStage> Stages,
    WistCliTraceResult Result);

internal sealed record WistCliTraceMetadata(
    string Tool,
    string Dialect,
    string Backend,
    bool SourceRedacted,
    bool RuntimeValuesRedacted);

internal sealed record WistCliSourceSummary(
    int Length,
    string Sha256);

internal sealed record WistCliTraceStage(
    string Id,
    string Kind,
    string Owner,
    string Status,
    IReadOnlyDictionary<string, string> Metadata);

internal sealed record WistCliTraceResult(
    string Status,
    string? ResultType,
    string? ErrorType,
    string? ErrorMessage);

internal sealed record WistCliTraceOptions(
    DateTimeOffset? CreatedAtUtc = null,
    int MaxMetadataValueLength = 256);
