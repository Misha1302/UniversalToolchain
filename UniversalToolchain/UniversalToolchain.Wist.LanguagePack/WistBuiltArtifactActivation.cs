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

    public static SsaRouteReport? GetSsaReport(
        LanguageRuntime runtime,
        LanguageArtifactBuildResult result)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(result);
        if (result.Backend == CilBackend)
            return runtime.GetBuiltArtifactValue(result, WistDirectBackendArtifactKinds.Cil).SsaReport;
        if (result.Backend == InterpreterBackend)
            return runtime.GetBuiltArtifactValue(result, WistDirectBackendArtifactKinds.Interpreter).SsaReport;
        throw new InvalidOperationException(
            $"Wist built artifact backend '{result.Backend.Value}' has no Wist optimization report projection.");
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
            for (var i = 0; i < arguments.Count; i++)
                ValidateAssignment(DeclaredBindings[i], arguments[i], i);
            return InvokeValidated(arguments);
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
