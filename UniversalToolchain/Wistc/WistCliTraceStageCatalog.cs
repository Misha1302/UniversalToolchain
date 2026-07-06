namespace Wistc;

internal static class WistCliTraceStageCatalog
{
    public static readonly WistCliTraceStageDescriptor Input =
        new("input", "TextInput", "Wistc");

    public static readonly WistCliTraceStageDescriptor DialectComposition =
        new("dialect-composition", "DialectComposition", "UniversalToolchain.Dialects");

    public static readonly WistCliTraceStageDescriptor RuntimeSelection =
        new("runtime-selection", "RuntimeSelection", "UniversalToolchain.Dialects");

    public static readonly WistCliTraceStageDescriptor BytecodeTranslation =
        new("bytecode-translation", "BytecodeTranslation", "BasicCore");

    public static readonly WistCliTraceStageDescriptor BackendArtifact =
        new("backend-artifact", "BackendArtifact", "UniversalToolchain.Runtime");

    public static readonly WistCliTraceStageDescriptor BackendExecution =
        new("backend-execution", "BackendExecution", "UniversalToolchain.Runtime");

    public static IReadOnlyList<WistCliTraceStageDescriptor> OrderedStages { get; } =
    [
        Input,
        DialectComposition,
        RuntimeSelection,
        BytecodeTranslation,
        BackendArtifact,
        BackendExecution
    ];
}

internal static class WistCliTraceStageStatus
{
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
}

internal sealed record WistCliTraceStageDescriptor(
    string Id,
    string Kind,
    string Owner);
