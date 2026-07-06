using BasicCore.Compilation;
using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Executes source through a selected dialect runtime plan without depending on a concrete reference language facade.
/// </summary>
public sealed class ToolchainRuntimeHost : IDisposable
{
    private readonly IToolchainRuntimeConfiguration _configuration;
    private readonly IToolchainArtifactExecutor _executor;
    private readonly IServiceProvider _serviceProvider;

    public ToolchainRuntimeHost(
        IServiceProvider serviceProvider,
        IToolchainRuntimeConfiguration configuration,
        IToolchainArtifactExecutor? executor = null)
    {
        _serviceProvider = serviceProvider.ArgNotNull();
        _configuration = configuration.ArgNotNull();
        _executor = executor ?? DefaultToolchainArtifactExecutor.Instance;
    }

    public IToolchainRuntimeConfiguration Configuration => _configuration;

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    public ICoreRunnable GetCore(string backend)
        => ResolveRuntime(backend).Core;

    public IArtifactCompiler GetArtifactCompiler(string backend)
        => ResolveRuntime(backend).ArtifactCompiler;

    /// <summary>
    ///     Gets a typed backend-specific artifact compiler for tests, benchmarks, and explicit fast-path adapters.
    /// </summary>
    public IArtifactCompiler<TCompilationOutput> GetBackendSpecificArtifactCompiler<TCompilationOutput>(string backend)
    {
        var runtime = ResolveRuntime(backend);

        if (runtime.Core is IArtifactCompiler<TCompilationOutput> artifactCompiler)
            return artifactCompiler;

        return Thrower.InvalidOpEx<IArtifactCompiler<TCompilationOutput>>(
            "Selected backend does not expose a compatible artifact compiler for the requested compilation output type.");
    }

    public ICompiledArtifact Compile(
        string code,
        OrderedDictionary<string, Type>? declaredBindings,
        string backend)
    {
        code = code.ArgNotNull();

        return GetArtifactCompiler(backend).Compile(code, declaredBindings);
    }

    public object? Run(
        string code,
        IReadOnlyDictionary<string, object?> arguments,
        OrderedDictionary<string, Type> declaredBindings,
        string backend)
    {
        code = code.ArgNotNull();
        arguments = arguments.ArgNotNull();
        declaredBindings = declaredBindings.ArgNotNull();

        var artifact = Compile(code, declaredBindings, backend);
        return _executor.Run(artifact, arguments);
    }

    public ToolchainRuntimeRunResult Run(ToolchainRuntimeRunRequest request)
    {
        request = request.ArgNotNull();

        var artifact = Compile(request.Code, request.DeclaredBindings, request.Backend);
        var value = _executor.Run(artifact, request.Arguments);
        return new ToolchainRuntimeRunResult(_configuration.DialectName, request.Backend, value);
    }

    public object? Run(string code, string backend) => GetCore(backend).Run(code);

    private ToolchainBackendRuntime ResolveRuntime(string backend)
    {
        if (string.IsNullOrWhiteSpace(backend))
            Thrower.Argument(nameof(backend), "Backend name must not be empty.");

        if (!_configuration.TryResolveKnownBackendId(backend, out var backendId))
        {
            var supportedBackends = string.Join(
                ", ",
                _configuration.EnabledBackends
                    .SelectMany(static x => x.AllNames)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal));
            Thrower.InvalidOpEx($"Unknown backend '{backend}'. Supported backends: {supportedBackends}.");
        }

        if (!_configuration.TryGetEnabledBackend(backendId, out var backendConfiguration))
            Thrower.InvalidOpEx($"Dialect '{_configuration.DialectName}' does not enable backend '{backend}'.");

        var runtime = _serviceProvider.GetServices<ToolchainBackendRuntime>()
            .FirstOrDefault(x => x.Descriptor.BackendId == backendConfiguration.BackendDescriptor.BackendId);
        if (runtime == null)
            Thrower.InvalidOpEx<ToolchainBackendRuntime>($"Backend core '{backendConfiguration.BackendDescriptor.CanonicalId}' was not registered.");

        return runtime;
    }
}
