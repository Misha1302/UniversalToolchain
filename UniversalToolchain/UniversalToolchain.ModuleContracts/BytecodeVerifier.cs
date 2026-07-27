namespace UniversalToolchain.ModuleContracts;

public sealed class BytecodeVerifier : IBytecodeVerifier
{
    public BytecodeVerificationResult Verify(BytecodeVerificationRequest request)
    {
        request = request.ArgNotNull();

        var severity = VerificationSeveritySelector.Select(request.Profile);
        var declaredTags = GetDeclaredTags(request.ContractTable);
        var declaredPatterns = GetDeclaredPatterns(request.ContractTable);
        var diagnostics = new List<ToolchainDiagnostic>();

        diagnostics.AddRange(VerifyInstructionTags(request.Bytecode, declaredTags, severity));
        diagnostics.AddRange(VerifyInstructionPatterns(
            request.Bytecode,
            declaredPatterns,
            severity));
        diagnostics.AddRange(VerifyObservedEmissions(
            request.ObservedEmissions ?? [],
            request.ContractTable,
            severity));

        var orderedDiagnostics = diagnostics
            .OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();

        return new BytecodeVerificationResult(
            orderedDiagnostics.All(static x => x.Severity != ToolchainDiagnosticSeverity.Error),
            orderedDiagnostics);
    }

    private static HashSet<string> GetDeclaredTags(SelectedModuleContractTable table) =>
        table.BytecodeFacets
            .SelectMany(static x => x.BytecodeEmissions)
            .SelectMany(static x => x.MayEmitTags)
            .Select(static x => x.Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> GetDeclaredPatterns(SelectedModuleContractTable table) =>
        table.BytecodeFacets
            .SelectMany(static x => x.BytecodeEmissions)
            .SelectMany(static x => x.MayEmitPatterns)
            .Select(static x => x.Value)
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<ToolchainDiagnostic> VerifyInstructionTags(
        Bytecode bytecode,
        HashSet<string> declaredTags,
        ToolchainDiagnosticSeverity severity)
    {
        foreach (var tag in ReadInstructionTags(bytecode).OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (declaredTags.Contains(tag))
                continue;

            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.UnknownBytecodeTag,
                severity,
                $"Bytecode tag '{tag}' is not declared by any selected module contract.",
                null,
                [new ToolchainDiagnosticHint("Declare the tag in a bytecode contract facet or declare it through typed contract metadata.")]);
        }
    }

    private static IEnumerable<ToolchainDiagnostic> VerifyInstructionPatterns(
        Bytecode bytecode,
        HashSet<string> declaredPatterns,
        ToolchainDiagnosticSeverity severity)
    {
        foreach (var pattern in ReadInstructionPatterns(bytecode)
                     .OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (declaredPatterns.Contains(pattern))
                continue;

            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.UnknownBytecodePattern,
                severity,
                $"Bytecode pattern '{pattern}' is not declared by any selected module contract.",
                null,
                [new ToolchainDiagnosticHint("Declare the emitted operation shape.")]);
        }
    }

    private static IEnumerable<string> ReadInstructionTags(Bytecode bytecode)
    {
        foreach (var instruction in bytecode.Instructions)
        {
            foreach (var tag in BytecodeContractMetadata.ReadSemanticTags(instruction))
                yield return tag.Value;

            foreach (var tag in instruction.Tags.Where(static tag => !BytecodeContractMetadata.IsContractMetadata(tag)))
                yield return tag;
        }
    }

    private static IEnumerable<string> ReadInstructionPatterns(Bytecode bytecode)
    {
        foreach (var instruction in bytecode.Instructions)
        {
            foreach (var pattern in BytecodeContractMetadata.ReadPatterns(instruction))
                yield return pattern.Value;
        }
    }

    private static IEnumerable<ToolchainDiagnostic> VerifyObservedEmissions(
        IReadOnlyList<ObservedBytecodeEmission> observedEmissions,
        SelectedModuleContractTable table,
        ToolchainDiagnosticSeverity severity)
    {
        var declarationsByModule = table.BytecodeFacets
            .GroupBy(static x => x.ModuleId)
            .ToDictionary(
                static x => x.Key,
                static x => x.SelectMany(static facet => facet.BytecodeEmissions).ToArray());

        foreach (var emission in observedEmissions)
        {
            declarationsByModule.TryGetValue(emission.ProducerModule, out var declarations);
            declarations ??= [];

            foreach (var tag in emission.Tags.Where(tag => declarations.All(declaration => !declaration.MayEmitTags.Contains(tag))))
                yield return CreateUndeclaredProducerDiagnostic(emission.ProducerModule, tag.Value, severity);

            foreach (var pattern in emission.Patterns.Where(pattern => declarations.All(declaration => !declaration.MayEmitPatterns.Contains(pattern))))
                yield return CreateUndeclaredProducerDiagnostic(emission.ProducerModule, pattern.Value, severity);

            if (emission.ObservedStackEffect == null)
                continue;

            foreach (var pattern in emission.Patterns)
            {
                var declaration = declarations.FirstOrDefault(x => x.MayEmitPatterns.Contains(pattern));
                if (declaration == null || !declaration.DeclaredStackEffect.IsKnown)
                    continue;

                if (declaration.DeclaredStackEffect == emission.ObservedStackEffect)
                    continue;

                yield return new ToolchainDiagnostic(
                    ModuleContractDiagnosticCodes.BytecodeStackEffectMismatch,
                    severity,
                    $"Bytecode pattern '{pattern}' declared stack effect '{declaration.DeclaredStackEffect}' but observed '{emission.ObservedStackEffect}'.",
                    null,
                    [new ToolchainDiagnosticHint("Update the descriptor or fix the lowerer emission shape.")]);
            }
        }
    }

    private static ToolchainDiagnostic CreateUndeclaredProducerDiagnostic(
        ModuleId moduleId,
        string emittedId,
        ToolchainDiagnosticSeverity severity) =>
        new(
            ModuleContractDiagnosticCodes.UndeclaredBytecodeProducer,
            severity,
            $"Module '{moduleId}' emitted undeclared bytecode id '{emittedId}'.",
            null,
            [new ToolchainDiagnosticHint("Add the id to the module bytecode facet or remove the emission.")]);
}
