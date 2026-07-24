namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Carries one adapter-owned structured model together with its deterministic source projection.
/// </summary>
public sealed class PlanFuzzProgram
{
    public PlanFuzzProgram(
        string modelKind,
        int modelSchemaVersion,
        PlanFuzzPayload model,
        string sourceText,
        PlanFuzzProgramClass programClass)
    {
        if (string.IsNullOrWhiteSpace(modelKind))
            Thrower.Argument(nameof(modelKind), "Program model kind must not be empty.");
        if (modelSchemaVersion <= 0)
            Thrower.Argument(nameof(modelSchemaVersion), "Program model schema version must be positive.");

        ModelKind = modelKind;
        ModelSchemaVersion = modelSchemaVersion;
        Model = model.ArgNotNull();
        SourceText = sourceText.ArgNotNull();
        ProgramClass = programClass;
    }

    public string ModelKind { get; }
    public int ModelSchemaVersion { get; }
    public PlanFuzzPayload Model { get; }
    public string SourceText { get; }
    public PlanFuzzProgramClass ProgramClass { get; }
}
