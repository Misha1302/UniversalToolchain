using BasicCore.Builtins;
using BasicCore.Contracts;
using BasicCore.Core;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Lowering;

namespace UniversalToolchain.Ssa.Optimization;

public enum SsaRoutePolicy
{
    Off,
    Prefer,
    Require,
    Debug
}

public sealed record SsaRouteDiagnostic(string Code, string Message)
{
    public SsaRouteDiagnostic(string code, string message, string? stage)
        : this(code, message)
    {
        Stage = string.IsNullOrWhiteSpace(stage) ? null : stage.Trim();
    }

    public string? Stage { get; init; }
}

public sealed record SsaRouteTraceEntry(string Stage, string Message, int? InstructionCount = null);

public sealed class SsaRouteReport
{
    public SsaRouteReport(
        SsaRoutePolicy policy,
        string profileId,
        bool usedSsa,
        bool fellBackToInput,
        int inputAirInstructionCount,
        int outputAirInstructionCount,
        IEnumerable<string>? executedPasses = null,
        IEnumerable<SsaRouteDiagnostic>? diagnostics = null,
        IEnumerable<SsaRouteTraceEntry>? trace = null)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "SSA route policy is not defined.");
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("SSA profile identifier must not be empty.", nameof(profileId));
        if (inputAirInstructionCount < 0)
            throw new ArgumentOutOfRangeException(nameof(inputAirInstructionCount));
        if (outputAirInstructionCount < 0)
            throw new ArgumentOutOfRangeException(nameof(outputAirInstructionCount));
        if (usedSsa && fellBackToInput)
            throw new ArgumentException("An SSA route cannot both succeed and fall back to its input.");
        if (policy == SsaRoutePolicy.Off && (usedSsa || fellBackToInput))
            throw new ArgumentException("A disabled SSA route cannot be reported as used or fallen back.");
        if (fellBackToInput && policy != SsaRoutePolicy.Prefer)
            throw new ArgumentException("Only the Prefer SSA policy may fall back to its input.");
        if (fellBackToInput && outputAirInstructionCount != inputAirInstructionCount)
        {
            throw new ArgumentException(
                "An SSA fallback must preserve the input AIR instruction count.",
                nameof(outputAirInstructionCount));
        }

        Policy = policy;
        ProfileId = profileId.Trim();
        UsedSsa = usedSsa;
        FellBackToInput = fellBackToInput;
        InputAirInstructionCount = inputAirInstructionCount;
        OutputAirInstructionCount = outputAirInstructionCount;
        ExecutedPasses = (executedPasses ?? []).ToArray();
        Diagnostics = (diagnostics ?? []).ToArray();
        Trace = (trace ?? []).ToArray();
    }

    public SsaRoutePolicy Policy { get; }

    public string ProfileId { get; }

    public bool UsedSsa { get; }

    public bool FellBackToInput { get; }

    public int InputAirInstructionCount { get; }

    public int OutputAirInstructionCount { get; }

    public IReadOnlyList<string> ExecutedPasses { get; }

    public IReadOnlyList<SsaRouteDiagnostic> Diagnostics { get; }

    public IReadOnlyList<SsaRouteTraceEntry> Trace { get; }
}

public sealed class SsaRouteResult
{
    public SsaRouteResult(IAbstractIR program, SsaRouteReport report)
    {
        Program = program ?? throw new ArgumentNullException(nameof(program));
        Report = report ?? throw new ArgumentNullException(nameof(report));
    }

    public IAbstractIR Program { get; }

    public SsaRouteReport Report { get; }

    public bool UsedSsa => Report.UsedSsa;

    public bool FellBackToInput => Report.FellBackToInput;

    public IReadOnlyList<SsaRouteDiagnostic> Diagnostics => Report.Diagnostics;
}

public sealed class SsaRouteException : InvalidOperationException
{
    public SsaRouteException(SsaRouteReport report, Exception? innerException = null)
        : base(
            "SSA route failed: " + string.Join("; ", report.Diagnostics.Select(static x => $"{x.Code}: {x.Message}")),
            innerException)
    {
        Report = report ?? throw new ArgumentNullException(nameof(report));
    }

    public SsaRouteReport Report { get; }

    public IReadOnlyList<SsaRouteDiagnostic> Diagnostics => Report.Diagnostics;
}

/// <summary>
/// Runs the verifier-gated AIR -&gt; SSA -&gt; optional profile optimization -&gt; AIR route.
/// </summary>
public sealed class SsaRoundtripRoute
{
    private const string UnprofiledId = "unprofiled-roundtrip";
    private const string LoweringStage = "lowering";
    private const string OptimizationStage = "optimization";
    private const string EmissionStage = "emission";
    private const string RouteStage = "route";
    private readonly AirToSsaConverter _lowering;
    private readonly SsaToAirConverter _emission;
    private readonly SsaRouteProfile? _profile;

    public SsaRoundtripRoute()
        : this(new AirToSsaConverter(), new SsaToAirConverter())
    {
    }

    public SsaRoundtripRoute(AirToSsaConverter lowering, SsaToAirConverter emission)
        : this(lowering, emission, profile: null)
    {
    }

    public SsaRoundtripRoute(AirToSsaConverter lowering, SsaToAirConverter emission, SsaRouteProfile? profile)
    {
        _lowering = lowering ?? throw new ArgumentNullException(nameof(lowering));
        _emission = emission ?? throw new ArgumentNullException(nameof(emission));
        _profile = profile;
    }

    public SsaRouteResult Run(IAbstractIR input, IrPipelineContext? context = null) =>
        Run(input, _profile?.Policy ?? SsaRoutePolicy.Prefer, context);

    public SsaRouteResult Run(IAbstractIR input, SsaRoutePolicy policy, IrPipelineContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "SSA route policy is not defined.");

        context ??= new IrPipelineContext();
        var profileId = _profile?.Id ?? UnprofiledId;
        var inputCount = input.Instructions.Count;
        var trace = CreateTrace(policy);
        trace?.Add(new SsaRouteTraceEntry("input", "Received AIR input.", inputCount));

        if (policy == SsaRoutePolicy.Off)
        {
            trace?.Add(new SsaRouteTraceEntry(RouteStage, "SSA route is disabled by policy.", inputCount));
            return new SsaRouteResult(
                input,
                new SsaRouteReport(
                    policy,
                    profileId,
                    usedSsa: false,
                    fellBackToInput: false,
                    inputCount,
                    inputCount,
                    trace: trace));
        }

        var effectiveContext = CreateEffectiveContext(context);
        var outcome = TryRoundtrip(input, policy, effectiveContext, trace);
        if (outcome.Output is not null)
        {
            var outputCount = outcome.Output.Instructions.Count;
            trace?.Add(new SsaRouteTraceEntry("output", "SSA route emitted structurally verified AIR.", outputCount));
            return new SsaRouteResult(
                outcome.Output,
                new SsaRouteReport(
                    policy,
                    profileId,
                    usedSsa: true,
                    fellBackToInput: false,
                    inputCount,
                    outputCount,
                    outcome.ExecutedPasses,
                    trace: trace));
        }

        trace?.Add(new SsaRouteTraceEntry("failure", "SSA route failed.", inputCount));
        var report = new SsaRouteReport(
            policy,
            profileId,
            usedSsa: false,
            fellBackToInput: policy == SsaRoutePolicy.Prefer,
            inputCount,
            inputCount,
            outcome.ExecutedPasses,
            outcome.Diagnostics,
            trace);

        if (policy == SsaRoutePolicy.Prefer)
            return new SsaRouteResult(input, report);

        throw new SsaRouteException(report, outcome.Exception);
    }

    private IrPipelineContext CreateEffectiveContext(IrPipelineContext context)
    {
        if (_profile is null || _profile.TargetCapabilities.Values.Count == 0)
            return context;

        var capabilities = new CapabilitySet(
            context.Capabilities.Values.Concat(_profile.TargetCapabilities.Values));
        return new IrPipelineContext(capabilities, context.Facts);
    }

    private RoundtripOutcome TryRoundtrip(
        IAbstractIR input,
        SsaRoutePolicy policy,
        IrPipelineContext context,
        List<SsaRouteTraceEntry>? trace)
    {
        var executedPasses = new List<string>();

        try
        {
            var normalizedInput = PrepareIntrinsicPayloads(input);
            if (!ReferenceEquals(normalizedInput, input))
            {
                trace?.Add(new SsaRouteTraceEntry(
                    "normalization",
                    "Prepared typed intrinsic payloads for SSA lowering.",
                    normalizedInput.Instructions.Count));
            }

            trace?.Add(new SsaRouteTraceEntry(LoweringStage, "Lowering AIR to SSA started.", normalizedInput.Instructions.Count));
            var loweringResult = _lowering.Run(new AirArtifact(normalizedInput), context);
            var ssaArtifact = loweringResult.Artifact.As<SsaArtifact>();
            var ssaFacts = loweringResult.Facts;
            trace?.Add(new SsaRouteTraceEntry(LoweringStage, "AIR to SSA lowering and verification succeeded."));

            if (_profile is not null)
            {
                var optimizer = SsaRouteFactory.CreateOptimizer(_profile);
                var plannedPasses = optimizer.PassIds.Select(static x => x.ToString()).ToArray();
                trace?.Add(new SsaRouteTraceEntry(
                    OptimizationStage,
                    plannedPasses.Length == 0
                        ? "SSA profile contains no optimization passes."
                        : $"Running SSA passes: {string.Join(", ", plannedPasses)}."));

                var optimizationResult = optimizer.Run(
                    ssaArtifact,
                    new IrPipelineContext(context.Capabilities, ssaFacts),
                    passId => executedPasses.Add(passId.ToString()));
                ssaArtifact = optimizationResult.Artifact.As<SsaArtifact>();
                ssaFacts = optimizationResult.Facts;
                trace?.Add(new SsaRouteTraceEntry(OptimizationStage, "SSA optimization and post-pass verification succeeded."));
            }

            trace?.Add(new SsaRouteTraceEntry(EmissionStage, "SSA to AIR emission started."));
            var emissionResult = _emission.Run(
                ssaArtifact,
                new IrPipelineContext(context.Capabilities, ssaFacts));
            var output = emissionResult.Artifact.As<AirArtifact>().Program;
            trace?.Add(new SsaRouteTraceEntry(EmissionStage, "SSA to AIR emission succeeded.", output.Instructions.Count));
            return new RoundtripOutcome(output, [], executedPasses, null);
        }
        catch (AirToSsaConversionException exception)
        {
            return new RoundtripOutcome(
                null,
                ConvertDiagnostics(exception.Diagnostics, LoweringStage),
                executedPasses,
                exception);
        }
        catch (SsaOptimizationException exception)
        {
            return new RoundtripOutcome(
                null,
                ConvertDiagnostics(exception.Diagnostics, OptimizationStage),
                executedPasses,
                exception);
        }
        catch (SsaToAirEmissionException exception)
        {
            return new RoundtripOutcome(
                null,
                ConvertDiagnostics(exception.Diagnostics, EmissionStage),
                executedPasses,
                exception);
        }
        catch (Exception exception) when (exception is not SsaRouteException)
        {
            // Unexpected defects must never be converted to a silent Prefer fallback.
            // Preserve a deterministic report so public hosts can observe the failed
            // stage while still surfacing the original exception as the inner cause.
            throw new SsaRouteException(
                new SsaRouteReport(
                    policy,
                    _profile?.Id ?? UnprofiledId,
                    usedSsa: false,
                    fellBackToInput: false,
                    input.Instructions.Count,
                    input.Instructions.Count,
                    executedPasses,
                    diagnostics:
                    [
                        new SsaRouteDiagnostic(
                            "ssa.route.unexpected",
                            $"Unexpected SSA route failure in '{exception.GetType().Name}': {exception.Message}",
                            RouteStage)
                    ],
                    trace: trace),
                exception);
        }
    }

    private static IAbstractIR PrepareIntrinsicPayloads(IAbstractIR input)
    {
        var changed = false;
        var prepared = new List<Instruction>(input.Instructions.Count);

        for (var index = 0; index < input.Instructions.Count; index++)
        {
            var instruction = input.Instructions[index];
            if (instruction.UOpCode != UOpCode.Intrinsic)
            {
                prepared.Add(CloneInstruction(instruction));
                continue;
            }

            if (!instruction.TryGetTypedIntrinsicInvocation(out var invocation))
            {
                throw new AirToSsaConversionException(
                [
                    new IrDiagnostic(
                        IrDiagnosticSeverity.Error,
                        "air.to-ssa.intrinsic.typed-payload-required",
                        $"AIR Intrinsic at instruction {index} must contain exactly one IntrinsicInvocation payload.")
                ]);
            }

            if (TryPrepareExternalLoad(invocation, instruction, out var externalLoad))
            {
                changed = true;
                prepared.Add(externalLoad);
                continue;
            }

            if (TryPrepareLoadConstant(invocation, instruction, out var push))
            {
                changed = true;
                prepared.Add(push);
                continue;
            }

            prepared.Add(CloneInstruction(instruction));
        }

        return changed ? new NormalizedAirProgram(prepared) : input;
    }

    private static bool TryPrepareExternalLoad(
        IntrinsicInvocation invocation,
        Instruction source,
        out Instruction prepared)
    {
        prepared = default!;
        if (invocation.Symbol != BuiltinIntrinsicSymbols.Core.LoadExternal ||
            invocation.DataOperands.Count != 1 ||
            invocation.DataOperands[0] is not int slot ||
            invocation.TypeArguments.Count != 1)
        {
            return false;
        }

        prepared = new Instruction(
            UOpCode.Push,
            [new AirExternalValueReference(slot, invocation.TypeArguments[0].RuntimeType)],
            [.. source.Metadata],
            source.Comment);
        return true;
    }

    private static bool TryPrepareLoadConstant(
        IntrinsicInvocation invocation,
        Instruction source,
        out Instruction prepared)
    {
        prepared = default!;
        if (invocation.Symbol != BuiltinIntrinsicSymbols.Core.LoadConst ||
            invocation.DataOperands.Count != 1 ||
            invocation.DataOperands[0] is not { } value)
        {
            return false;
        }

        prepared = new Instruction(
            UOpCode.Push,
            [value],
            [.. source.Metadata],
            source.Comment);
        return true;
    }

    private static Instruction CloneInstruction(Instruction instruction) =>
        new(instruction.UOpCode, [.. instruction.Operands], [.. instruction.Metadata], instruction.Comment);

    private sealed class NormalizedAirProgram(IReadOnlyList<Instruction> instructions) : IAbstractIR
    {
        private readonly List<Instruction> _instructions = instructions.Select(CloneInstruction).ToList();

        public IReadOnlyList<Instruction> Instructions => _instructions;

        public void Nop() => throw Immutable();
        public void Push<T>(T value) => throw Immutable();
        public void Drop() => throw Immutable();
        public void Jmp(Guid identifier) => throw Immutable();
        public void JmpIf(Guid identifier) => throw Immutable();
        public void JmpIfNot(Guid identifier) => throw Immutable();
        public void SetLabel(Guid label) => throw Immutable();
        public void Annotate(params List<object>[] annotations) => throw Immutable();
        public void Intrinsic(string capabilityId, params object?[] dataOperands) => throw Immutable();
        public void AppendInstructions(IReadOnlyList<Instruction> instructions) => throw Immutable();

        private static InvalidOperationException Immutable() =>
            new("The normalized SSA route input is immutable.");
    }

    private List<SsaRouteTraceEntry>? CreateTrace(SsaRoutePolicy policy)
    {
        var diagnosticMode = _profile?.Diagnostics ?? SsaDiagnosticMode.Default;
        return policy == SsaRoutePolicy.Debug || diagnosticMode == SsaDiagnosticMode.Verbose
            ? []
            : null;
    }

    private static IReadOnlyList<SsaRouteDiagnostic> ConvertDiagnostics(
        IEnumerable<IrDiagnostic> diagnostics,
        string stage) =>
        diagnostics.Select(diagnostic => new SsaRouteDiagnostic(
            diagnostic.Code,
            diagnostic.Message,
            stage)).ToArray();

    private sealed record RoundtripOutcome(
        IAbstractIR? Output,
        IReadOnlyList<SsaRouteDiagnostic> Diagnostics,
        IReadOnlyList<string> ExecutedPasses,
        Exception? Exception);
}
