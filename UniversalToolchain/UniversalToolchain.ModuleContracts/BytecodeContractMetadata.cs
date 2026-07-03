namespace UniversalToolchain.ModuleContracts;

public static class BytecodeContractMetadata
{
    public const string Prefix = "contract.";
    public const string ProducerModulePrefix = Prefix + "producer-module:";
    public const string SourceNodePrefix = Prefix + "source-node:";
    public const string PatternPrefix = Prefix + "bytecode-pattern:";
    public const string SemanticTagPrefix = Prefix + "bytecode-tag:";

    public static string ProducerModule(ModuleId moduleId) => ProducerModulePrefix + moduleId.Value;

    public static string SourceNode(AstNodeKind nodeKind) => SourceNodePrefix + nodeKind.Value;

    public static string Pattern(BytecodePatternId patternId) => PatternPrefix + patternId.Value;

    public static string SemanticTag(BytecodeTagId tagId) => SemanticTagPrefix + tagId.Value;

    public static bool IsContractMetadata(string tag) =>
        tag.StartsWith(Prefix, StringComparison.Ordinal);

    public static bool TryReadProducerModule(BytecodeInstruction instruction, out ModuleId moduleId)
    {
        moduleId = default;
        var value = TryReadSingleValue(instruction, ProducerModulePrefix, out var singleValue)
            ? singleValue
            : null;
        if (value == null)
            return false;

        moduleId = new ModuleId(value);
        return true;
    }

    public static bool TryReadSourceNode(BytecodeInstruction instruction, out AstNodeKind nodeKind)
    {
        nodeKind = default;
        var value = TryReadSingleValue(instruction, SourceNodePrefix, out var singleValue)
            ? singleValue
            : null;
        if (value == null)
            return false;

        nodeKind = new AstNodeKind(value);
        return true;
    }

    public static IReadOnlyList<BytecodePatternId> ReadPatterns(BytecodeInstruction instruction) =>
        ReadValues(instruction, PatternPrefix)
            .Select(static x => new BytecodePatternId(x))
            .ToArray();

    public static IReadOnlyList<BytecodeTagId> ReadSemanticTags(BytecodeInstruction instruction) =>
        ReadValues(instruction, SemanticTagPrefix)
            .Select(static x => new BytecodeTagId(x))
            .ToArray();

    public static IReadOnlyList<ToolchainDiagnostic> Validate(BytecodeInstruction instruction)
    {
        instruction = instruction.ArgNotNull();

        return ValidateSingleValue(instruction, ProducerModulePrefix, "producer module")
            .Concat(ValidateSingleValue(instruction, SourceNodePrefix, "source node"))
            .ToArray();
    }

    private static bool TryReadSingleValue(
        BytecodeInstruction instruction,
        string prefix,
        out string? value)
    {
        var values = ReadValues(instruction, prefix);
        value = values.Count == 1 ? values[0] : null;
        return values.Count <= 1;
    }

    private static IEnumerable<ToolchainDiagnostic> ValidateSingleValue(
        BytecodeInstruction instruction,
        string prefix,
        string displayName)
    {
        var values = ReadValues(instruction, prefix);
        if (values.Count <= 1)
            yield break;

        yield return new ToolchainDiagnostic(
            ModuleContractDiagnosticCodes.InvalidBytecodeContractMetadata,
            ToolchainDiagnosticSeverity.Error,
            $"Bytecode instruction '{instruction}' declares multiple contract {displayName} metadata values: {string.Join(", ", values)}.",
            null,
            [new ToolchainDiagnosticHint("Emit exactly one producer module and one source node metadata value per contract-annotated instruction.")]);
    }

    private static IReadOnlyList<string> ReadValues(BytecodeInstruction instruction, string prefix) =>
        instruction.Tags
            .Where(tag => tag.StartsWith(prefix, StringComparison.Ordinal))
            .Select(tag => tag[prefix.Length..])
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(static x => x, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
