namespace UniversalToolchain.ModuleContracts;

public sealed class AirVerifier : IAirVerifier
{
    private readonly AirStackDisciplineVerifier _stackVerifier;

    public AirVerifier()
        : this(CreateDefaultIntrinsicReader(), CreateDefaultIntrinsicProcessor())
    {
    }

    public AirVerifier(
        IInstructionIntrinsicReader intrinsicReader,
        IIntrinsicTypeStackProcessor intrinsicTypeStackProcessor)
    {
        _stackVerifier = new AirStackDisciplineVerifier(
            intrinsicReader.ArgNotNull(),
            intrinsicTypeStackProcessor.ArgNotNull());
    }

    public AirVerificationResult Verify(AirVerificationRequest request)
    {
        request = request.ArgNotNull();

        var severity = VerificationSeveritySelector.Select(request.Profile);
        var diagnostics = new List<ToolchainDiagnostic>();

        diagnostics.AddRange(VerifySelectedCapabilitiesExist(
            request.ContractTable,
            request.BackendSelection,
            severity));

        var schemaDiagnostics = VerifyInstructionSchemas(request.Air, severity).ToArray();
        var branchDiagnostics = VerifyBranchTargets(request.Air, severity).ToArray();
        diagnostics.AddRange(schemaDiagnostics);
        diagnostics.AddRange(branchDiagnostics);

        if (schemaDiagnostics.Length == 0 && branchDiagnostics.Length == 0)
            diagnostics.AddRange(_stackVerifier.Verify(request.Air, severity));

        diagnostics.AddRange(VerifyIntrinsicSupport(
            request.Air,
            request.ContractTable,
            request.BackendSelection,
            severity));

        var orderedDiagnostics = diagnostics
            .OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();

        return new AirVerificationResult(
            orderedDiagnostics.All(static x => x.Severity != ToolchainDiagnosticSeverity.Error),
            orderedDiagnostics);
    }

    private static IEnumerable<ToolchainDiagnostic> VerifySelectedCapabilitiesExist(
        SelectedModuleContractTable table,
        BackendCapabilitySelection selection,
        ToolchainDiagnosticSeverity severity)
    {
        var declaredCapabilities = table.BackendCapabilityFacets
            .SelectMany(static x => x.Capabilities)
            .Select(static x => x.CapabilityId)
            .ToHashSet();

        foreach (var capability in selection.CapabilityIds.Where(capability => !declaredCapabilities.Contains(capability)))
        {
            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.UnknownBackendCapability,
                severity,
                $"Selected backend capability '{capability}' is not declared by any selected module contract.",
                null,
                [new ToolchainDiagnosticHint("Declare backend capabilities through backend capability facets before selecting them.")]);
        }
    }

    private static IEnumerable<ToolchainDiagnostic> VerifyInstructionSchemas(
        IAbstractIR air,
        ToolchainDiagnosticSeverity severity)
    {
        for (var index = 0; index < air.Instructions.Count; index++)
        {
            var instruction = air.Instructions[index];

            if (!Enum.IsDefined(instruction.UOpCode))
            {
                yield return CreateSchemaDiagnostic(index, instruction, "unknown opcode", severity);
                continue;
            }

            var isValid = instruction.UOpCode switch
            {
                UOpCode.Nop or UOpCode.Drop => instruction.Operands.Count == 0,
                UOpCode.Push => instruction.Operands.Count == 1 && instruction.Operands[0] is not null,
                UOpCode.Jmp or UOpCode.JmpIf or UOpCode.JmpIfNot or UOpCode.Label =>
                    instruction.Operands.Count == 1 && instruction.Operands[0] is Guid,
                UOpCode.Annotate => true,
                UOpCode.Intrinsic => TryReadIntrinsicId(instruction, out _),
                _ => false
            };

            if (isValid)
                continue;

            yield return CreateSchemaDiagnostic(index, instruction, "invalid operand schema", severity);
        }
    }

    private static IEnumerable<ToolchainDiagnostic> VerifyBranchTargets(
        IAbstractIR air,
        ToolchainDiagnosticSeverity severity)
    {
        var labels = air.Instructions
            .Where(static x => x.UOpCode == UOpCode.Label && x.Operands.Count == 1 && x.Operands[0] is Guid)
            .Select(static x => (Guid)x.Operands[0]!)
            .GroupBy(static x => x)
            .ToArray();
        var labelSet = labels.Select(static x => x.Key).ToHashSet();

        foreach (var duplicateLabel in labels.Where(static x => x.Count() > 1).Select(static x => x.Key))
        {
            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.DuplicateAirLabel,
                severity,
                $"AIR label '{duplicateLabel}' is declared more than once.",
                null,
                [new ToolchainDiagnosticHint("Emit each AIR label id once before backend lowering.")]);
        }

        foreach (var instruction in air.Instructions.Where(IsBranchInstruction))
        {
            if (instruction.Operands.Count != 1 || instruction.Operands[0] is not Guid target)
                continue;

            if (labelSet.Contains(target))
                continue;

            yield return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.MissingAirBranchTarget,
                severity,
                $"AIR branch target '{target}' does not match any emitted label.",
                null,
                [new ToolchainDiagnosticHint("Emit a matching label instruction or remove the branch.")]);
        }
    }

    private static IEnumerable<ToolchainDiagnostic> VerifyIntrinsicSupport(
        IAbstractIR air,
        SelectedModuleContractTable table,
        BackendCapabilitySelection selection,
        ToolchainDiagnosticSeverity severity)
    {
        var supportedIntrinsics = selection.SupportedIntrinsics.ToHashSet();
        var universalInterpreterIntrinsics = selection.Policy.UniversalIntrinsicAllowList.ToHashSet();
        var selectedCapabilities = selection.CapabilityIds.ToHashSet();
        var requiredCapabilitiesByIntrinsic = BuildRequiredCapabilitiesByIntrinsic(table);

        foreach (var instruction in air.Instructions.Where(static x => x.UOpCode == UOpCode.Intrinsic))
        {
            if (!TryReadIntrinsicId(instruction, out var intrinsic))
                continue;

            if (!supportedIntrinsics.Contains(intrinsic))
            {
                yield return new ToolchainDiagnostic(
                    ModuleContractDiagnosticCodes.UnsupportedAirIntrinsic,
                    severity,
                    $"AIR intrinsic '{intrinsic}' is not supported by the selected backend capabilities.",
                    null,
                    [new ToolchainDiagnosticHint("Select a backend capability that supports the intrinsic or keep the optimizer from emitting it.")]);
            }

            if (selection.Policy.RejectNonUniversalIntrinsics && !universalInterpreterIntrinsics.Contains(intrinsic))
            {
                yield return new ToolchainDiagnostic(
                    ModuleContractDiagnosticCodes.InterpreterBackendIntrinsicViolation,
                    severity,
                    $"AIR intrinsic '{intrinsic}' is not part of the universal interpreter intrinsic surface.",
                    null,
                    [new ToolchainDiagnosticHint("Use universal calls for interpreter-selected plans or route this AIR through a backend that declares the intrinsic.")]);
            }

            if (!requiredCapabilitiesByIntrinsic.TryGetValue(intrinsic, out var requiredCapabilities))
                continue;

            foreach (var requiredCapability in requiredCapabilities.Where(requiredCapability => !selectedCapabilities.Contains(requiredCapability)))
            {
                yield return new ToolchainDiagnostic(
                    ModuleContractDiagnosticCodes.MissingBackendCapability,
                    severity,
                    $"AIR intrinsic '{intrinsic}' requires backend capability '{requiredCapability}', but it is not selected.",
                    null,
                    [new ToolchainDiagnosticHint("Select the required backend capability or emit a backend-neutral AIR form.")]);
            }
        }
    }

    private static Dictionary<IntrinsicSymbolId, IReadOnlyList<BackendCapabilityId>> BuildRequiredCapabilitiesByIntrinsic(
        SelectedModuleContractTable table)
    {
        return table.AirFacets
            .SelectMany(static x => x.AirEmissions)
            .SelectMany(static emission => emission.MayEmitIntrinsics.Select(intrinsic => (intrinsic, emission.RequiredCapabilities)))
            .GroupBy(static x => x.intrinsic)
            .ToDictionary(
                static x => x.Key,
                static x => (IReadOnlyList<BackendCapabilityId>)x
                    .SelectMany(static item => item.RequiredCapabilities)
                    .OrderBy(static item => item.Value, StringComparer.Ordinal)
                    .Distinct()
                    .ToArray());
    }

    private static bool TryReadIntrinsicId(Instruction instruction, out IntrinsicSymbolId intrinsicId)
    {
        intrinsicId = default;

        if (instruction.UOpCode != UOpCode.Intrinsic || instruction.Operands.Count == 0)
            return false;

        if (instruction.Operands[0] is string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            intrinsicId = new IntrinsicSymbolId(value);
            return true;
        }

        if (instruction.Operands.Count != 1 || instruction.Operands[0] is not IntrinsicInvocation invocation)
            return false;

        intrinsicId = IntrinsicCapabilityNameEncoder.TryEncode(invocation.Symbol, invocation.TypeArguments, out var encodedName)
            ? new IntrinsicSymbolId(encodedName)
            : new IntrinsicSymbolId(invocation.Symbol.ToString());
        return true;
    }

    private static bool IsBranchInstruction(Instruction instruction) =>
        instruction.UOpCode is UOpCode.Jmp or UOpCode.JmpIf or UOpCode.JmpIfNot;

    private static ToolchainDiagnostic CreateSchemaDiagnostic(
        int instructionIndex,
        Instruction instruction,
        string reason,
        ToolchainDiagnosticSeverity severity) =>
        new(
            ModuleContractDiagnosticCodes.InvalidAirOperandSchema,
            severity,
            $"AIR instruction at index {instructionIndex} has {reason} for opcode '{instruction.UOpCode}'.",
            null,
            [new ToolchainDiagnosticHint("Emit AIR through the structured AIR builder or match the opcode operand contract before verification.")]);

    private static IInstructionIntrinsicReader CreateDefaultIntrinsicReader() => new InstructionIntrinsicReader();

    private static IIntrinsicTypeStackProcessor CreateDefaultIntrinsicProcessor()
    {
        var typeContext = new IntrinsicTypeResolutionContext();
        var provider = new BasicCore.Builtins.CoreIntrinsicDescriptorProvider(
            new MethodCallTypeSemanticsResolver());
        var catalog = new IntrinsicCatalogBuilder().Build([provider]);
        return new IntrinsicTypeStackProcessor(catalog, typeContext);
    }

}
