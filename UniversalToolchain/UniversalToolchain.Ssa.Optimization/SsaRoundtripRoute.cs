using IntermediateRepresentationAbstractions;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
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

public sealed record SsaRouteDiagnostic(string Code, string Message);

public sealed class SsaRouteResult
{
    public SsaRouteResult(
        IAbstractIR program,
        bool usedSsa,
        bool fellBackToInput,
        IEnumerable<SsaRouteDiagnostic>? diagnostics = null)
    {
        Program = program ?? throw new ArgumentNullException(nameof(program));
        UsedSsa = usedSsa;
        FellBackToInput = fellBackToInput;
        Diagnostics = (diagnostics ?? []).ToArray();
    }

    public IAbstractIR Program { get; }

    public bool UsedSsa { get; }

    public bool FellBackToInput { get; }

    public IReadOnlyList<SsaRouteDiagnostic> Diagnostics { get; }
}

public sealed class SsaRouteException : InvalidOperationException
{
    public SsaRouteException(IEnumerable<SsaRouteDiagnostic> diagnostics)
        : this((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray())
    {
    }

    private SsaRouteException(IReadOnlyList<SsaRouteDiagnostic> diagnostics)
        : base("SSA route failed: " + string.Join("; ", diagnostics.Select(static x => $"{x.Code}: {x.Message}")))
    {
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<SsaRouteDiagnostic> Diagnostics { get; }
}

/// <summary>
/// Runs the verifier-gated AIR -> SSA -> AIR route without applying SSA optimization passes.
/// </summary>
public sealed class SsaRoundtripRoute
{
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

        context ??= new IrPipelineContext();
        if (policy == SsaRoutePolicy.Off)
            return new SsaRouteResult(input, usedSsa: false, fellBackToInput: false);

        var route = TryRoundtrip(input, context, out var output, out var diagnostics);
        if (route)
            return new SsaRouteResult(output!, usedSsa: true, fellBackToInput: false);

        if (policy == SsaRoutePolicy.Prefer)
            return new SsaRouteResult(input, usedSsa: false, fellBackToInput: true, diagnostics);

        throw new SsaRouteException(diagnostics);
    }

    private bool TryRoundtrip(
        IAbstractIR input,
        IrPipelineContext context,
        out IAbstractIR? output,
        out IReadOnlyList<SsaRouteDiagnostic> diagnostics)
    {
        output = null;
        diagnostics = [];

        try
        {
            var loweringResult = _lowering.Run(new AirArtifact(input), context);
            var ssaArtifact = loweringResult.Artifact.As<SsaArtifact>();
            var emissionResult = _emission.Run(ssaArtifact, new IrPipelineContext(context.Capabilities, loweringResult.Facts));
            output = emissionResult.Artifact.As<AirArtifact>().Program;
            return true;
        }
        catch (AirToSsaConversionException exception)
        {
            diagnostics = ConvertDiagnostics(exception.Diagnostics);
            return false;
        }
        catch (SsaToAirEmissionException exception)
        {
            diagnostics = ConvertDiagnostics(exception.Diagnostics);
            return false;
        }
    }

    private static IReadOnlyList<SsaRouteDiagnostic> ConvertDiagnostics(IEnumerable<IrDiagnostic> diagnostics) =>
        diagnostics.Select(static x => new SsaRouteDiagnostic(x.Code, x.Message)).ToArray();
}
