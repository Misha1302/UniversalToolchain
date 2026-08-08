using BasicCilCompiler.Execution;
using BasicCore.Compilation;
using BasicCore.Execution;
using BasicInterpreter;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist.LanguagePack;

internal interface IWistDurableProgram
{
    IReadOnlyList<ExternalBinding> DeclaredBindings { get; }
    bool TryCreateNativeDelegate(Type delegateType, out Delegate? compiledDelegate);
    object? Invoke(IReadOnlyList<object?> arguments);
}

internal sealed record WistSsaDiagnosticSnapshot(string Code, string Message, string? Stage);
internal sealed record WistSsaTraceSnapshot(string Stage, string Message, int? InstructionCount);

internal sealed class WistSsaReportSnapshot
{
    public WistSsaReportSnapshot(
        bool usedSsa,
        bool fellBackToInput,
        string profileId,
        int inputAirInstructionCount,
        int outputAirInstructionCount,
        IEnumerable<string> executedPasses,
        IEnumerable<WistSsaDiagnosticSnapshot> diagnostics,
        IEnumerable<WistSsaTraceSnapshot> trace)
    {
        UsedSsa = usedSsa;
        FellBackToInput = fellBackToInput;
        ProfileId = profileId ?? throw new ArgumentNullException(nameof(profileId));
        InputAirInstructionCount = inputAirInstructionCount;
        OutputAirInstructionCount = outputAirInstructionCount;
        ExecutedPasses = executedPasses?.ToArray() ?? throw new ArgumentNullException(nameof(executedPasses));
        Diagnostics = diagnostics?.ToArray() ?? throw new ArgumentNullException(nameof(diagnostics));
        Trace = trace?.ToArray() ?? throw new ArgumentNullException(nameof(trace));
    }

    public bool UsedSsa { get; }
    public bool FellBackToInput { get; }
    public string ProfileId { get; }
    public int InputAirInstructionCount { get; }
    public int OutputAirInstructionCount { get; }
    public IReadOnlyList<string> ExecutedPasses { get; }
    public IReadOnlyList<WistSsaDiagnosticSnapshot> Diagnostics { get; }
    public IReadOnlyList<WistSsaTraceSnapshot> Trace { get; }
}

internal static class WistBuiltArtifactActivation
{
    private static readonly BackendId CilBackend = new("cil");
    private static readonly BackendId InterpreterBackend = new("interpreter");

    public static IWistDurableProgram Materialize(
        LanguageRuntime runtime,
        LanguageArtifactBuildResult result)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(result);
        if (result.Backend == CilBackend)
        {
            var artifact = runtime.GetBuiltArtifactValue(result, WistDirectBackendArtifactKinds.Cil);
            return new CilProgram(artifact, runtime.Plan);
        }
        if (result.Backend == InterpreterBackend)
        {
            var artifact = runtime.GetBuiltArtifactValue(result, WistDirectBackendArtifactKinds.Interpreter);
            return new InterpreterProgram(artifact, runtime.Plan);
        }
        throw new InvalidOperationException(
            $"Wist built artifact backend '{result.Backend.Value}' cannot be materialized as a durable program.");
    }

    public static WistSsaReportSnapshot? GetSsaReport(
        LanguageRuntime runtime,
        LanguageArtifactBuildResult result)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(result);
        SsaRouteReport? report;
        if (result.Backend == CilBackend)
            report = runtime.GetBuiltArtifactValue(result, WistDirectBackendArtifactKinds.Cil).SsaReport;
        else if (result.Backend == InterpreterBackend)
            report = runtime.GetBuiltArtifactValue(result, WistDirectBackendArtifactKinds.Interpreter).SsaReport;
        else
            throw new InvalidOperationException(
                $"Wist built artifact backend '{result.Backend.Value}' has no Wist optimization report projection.");
        return Project(report);
    }

    public static WistSsaReportSnapshot? TryGetSsaReport(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is SsaRouteException ssa)
                return Project(ssa.Report);
        }
        return null;
    }

    private static WistSsaReportSnapshot? Project(SsaRouteReport? report)
    {
        if (report == null)
            return null;
        return new WistSsaReportSnapshot(
            report.UsedSsa,
            report.FellBackToInput,
            report.ProfileId,
            report.InputAirInstructionCount,
            report.OutputAirInstructionCount,
            report.ExecutedPasses,
            report.Diagnostics.Select(static diagnostic =>
                new WistSsaDiagnosticSnapshot(diagnostic.Code, diagnostic.Message, diagnostic.Stage)),
            report.Trace.Select(static entry =>
                new WistSsaTraceSnapshot(entry.Stage, entry.Message, entry.InstructionCount)));
    }

    private abstract class ProgramBase(IReadOnlyList<ExternalBinding> bindings, LanguagePlan plan) : IWistDurableProgram
    {
        protected LanguagePlan Plan { get; } = plan ?? throw new ArgumentNullException(nameof(plan));
        public IReadOnlyList<ExternalBinding> DeclaredBindings { get; } = bindings?.ToArray()
            ?? throw new ArgumentNullException(nameof(bindings));

        public virtual bool TryCreateNativeDelegate(Type delegateType, out Delegate? compiledDelegate)
        {
            compiledDelegate = null;
            return false;
        }

        public object? Invoke(IReadOnlyList<object?> arguments)
        {
            ArgumentNullException.ThrowIfNull(arguments);
            if (arguments.Count != DeclaredBindings.Count)
            {
                throw new ArgumentException(
                    $"Compiled Wist program expects {DeclaredBindings.Count} arguments, but {arguments.Count} were supplied.",
                    nameof(arguments));
            }

            var normalized = new object?[arguments.Count];
            for (var i = 0; i < arguments.Count; i++)
            {
                normalized[i] = WistRuntimeValueAdapterActivation.NormalizeInput(Plan, arguments[i]);
                ValidateAssignment(DeclaredBindings[i], normalized[i], i);
            }
            return InvokeValidated(normalized);
        }

        protected abstract object? InvokeValidated(IReadOnlyList<object?> arguments);

        protected static ExecutionEnvironment CreateEnvironment(
            IReadOnlyList<ExternalBinding> bindings,
            IReadOnlyList<object?> arguments)
        {
            var environment = new ExecutionEnvironment(bindings);
            for (var i = 0; i < arguments.Count; i++)
                environment.SetExternalValue(i, arguments[i]);
            return environment;
        }

        private static void ValidateAssignment(ExternalBinding binding, object? value, int slot)
        {
            if (binding.Kind == ExternalBindingKind.Constant)
                throw new InvalidOperationException($"Binding '{binding.Name}' at slot {slot} is constant.");
            if (value == null)
            {
                if (binding.Type.IsValueType && Nullable.GetUnderlyingType(binding.Type) == null)
                {
                    throw new ArgumentException(
                        $"Null cannot be assigned to non-nullable argument '{binding.Name}' ({binding.Type}).");
                }
                return;
            }
            if (!binding.Type.IsInstanceOfType(value))
            {
                throw new ArgumentException(
                    $"Value of type '{value.GetType()}' is not assignable to argument '{binding.Name}' with declared type '{binding.Type}'.");
            }
        }
    }

    private sealed class InterpreterProgram(WistInterpreterArtifact artifact, LanguagePlan plan)
        : ProgramBase(artifact.Input.ExternalBindings, plan)
    {
        protected override object? InvokeValidated(IReadOnlyList<object?> arguments)
        {
            var environment = CreateEnvironment(artifact.Input.ExternalBindings, arguments);
            var value = new InterpreterImpl().Execute(artifact.Air, environment);
            return WistRuntimeValueAdapterActivation.Normalize(Plan, value);
        }
    }

    private sealed class CilProgram(WistCilArtifact artifact, LanguagePlan plan)
        : ProgramBase(artifact.Input.ExternalBindings, plan)
    {
        public override bool TryCreateNativeDelegate(Type delegateType, out Delegate? compiledDelegate)
        {
            ArgumentNullException.ThrowIfNull(delegateType);
            var invoke = delegateType.GetMethod("Invoke")
                ?? throw new ArgumentException($"Type '{delegateType}' is not a delegate type.", nameof(delegateType));
            var parameters = invoke.GetParameters();
            var methodParameters = artifact.Compilation.Method.GetParameters();
            if (parameters.Length != methodParameters.Length ||
                !parameters.Select(static parameter => parameter.ParameterType)
                    .SequenceEqual(methodParameters.Select(static parameter => parameter.ParameterType)) ||
                invoke.ReturnType != artifact.Compilation.Method.ReturnType)
            {
                compiledDelegate = null;
                return false;
            }

            compiledDelegate = artifact.Compilation.HasConstantPool
                ? artifact.Compilation.Method.CreateDelegate(delegateType, artifact.Compilation.ConstantPool)
                : artifact.Compilation.Method.CreateDelegate(delegateType);
            return true;
        }

        protected override object? InvokeValidated(IReadOnlyList<object?> arguments)
        {
            var environment = CreateEnvironment(artifact.Input.ExternalBindings, arguments);
            var value = new DynamicMethodExecutor().Execute(artifact.Compilation, environment);
            return WistRuntimeValueAdapterActivation.Normalize(Plan, value);
        }
    }
}
