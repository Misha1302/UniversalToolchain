using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

/// <summary>
/// Runs the verifier-gated preview SSA optimization route as an opt-in dialect optimizer.
/// </summary>
[DialectOptimizerAlias("SsaConstantFolding", "SsaPreviewOptimization", "SsaPreview")]
[DialectRuntimeExport("Optimizer", "Ssa")]
public sealed class SsaPreviewOptimizerModule : IIRProcessingModule
{
    private readonly SsaRuntimeExecutionOptions _options;
    private readonly ISsaRouteReportSink _reportSink;
    private readonly IReadOnlyList<ISsaManagedCallableProjection> _managedCallableProjections;

    public SsaPreviewOptimizerModule()
        : this(SsaRuntimeExecutionOptions.RequireDefault, NullSsaRouteReportSink.Instance, [])
    {
    }

    public SsaPreviewOptimizerModule(
        SsaRuntimeExecutionOptions options,
        ISsaRouteReportSink reportSink,
        IEnumerable<ISsaManagedCallableProjection> managedCallableProjections)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).SnapshotValidated();
        _reportSink = reportSink ?? throw new ArgumentNullException(nameof(reportSink));
        _managedCallableProjections = (managedCallableProjections ?? throw new ArgumentNullException(nameof(managedCallableProjections))).ToArray();
    }

    public IAbstractIR ProcessIr<TCompilationOutput>(
        IAbstractIR current,
        IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(compiler);

        var profile = SsaPreviewRouteProfiles.Create(
            _options.Policy,
            _options.Diagnostics,
            _options.TargetCapabilities,
            _options.ProfileId);
        var route = SsaRouteFactory.CreateRoundtripRoute(profile, _managedCallableProjections);

        try
        {
            var result = route.Run(current);
            _reportSink.Publish(result.Report);
            return result.Program;
        }
        catch (SsaRouteException exception)
        {
            _reportSink.Publish(exception.Report);
            throw;
        }
    }
}
