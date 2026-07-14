using BasicCore.Compilation;
using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Executes source through a selected dialect runtime plan without depending on a concrete reference language facade.
/// </summary>
public sealed class ToolchainRuntimeHost : IDisposable
{
    private readonly IToolchainRuntimeConfiguration _configuration;
    private readonly IToolchainArtifactExecutor _executor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyDictionary<DialectBackendId, ToolchainBackendRuntimeRegistration> _runtimeRegistrations;
    private readonly ServiceProviderOwnership _serviceProviderOwnership;
    private bool _disposed;

    public ToolchainRuntimeHost(
        IServiceProvider serviceProvider,
        IToolchainRuntimeConfiguration configuration,
        IToolchainArtifactExecutor? executor = null)
        : this(serviceProvider, configuration, ServiceProviderOwnership.Borrowed, executor)
    {
    }

    public ToolchainRuntimeHost(
        IServiceProvider serviceProvider,
        IToolchainRuntimeConfiguration configuration,
        ServiceProviderOwnership serviceProviderOwnership,
        IToolchainArtifactExecutor? executor = null)
    {
        _serviceProvider = serviceProvider.ArgNotNull();
        _configuration = configuration.ArgNotNull();
        _serviceProviderOwnership = serviceProviderOwnership;
        _executor = executor ?? DefaultToolchainArtifactExecutor.Instance;
        _runtimeRegistrations = BuildRuntimeRegistrationMap(_serviceProvider);
    }

    public IToolchainRuntimeConfiguration Configuration => _configuration;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_serviceProviderOwnership == ServiceProviderOwnership.Owned &&
            _serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
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

        if (!_runtimeRegistrations.TryGetValue(
                backendConfiguration.BackendDescriptor.BackendId,
                out var registration))
        {
            Thrower.InvalidOpEx<ToolchainBackendRuntime>(
                $"Backend core '{backendConfiguration.BackendDescriptor.CanonicalId}' was not registered.");
        }

        return registration.Resolve(_serviceProvider);
    }

    private static IReadOnlyDictionary<DialectBackendId, ToolchainBackendRuntimeRegistration> BuildRuntimeRegistrationMap(
        IServiceProvider serviceProvider)
    {
        var registrations = serviceProvider
            .GetService<IEnumerable<ToolchainBackendRuntimeRegistration>>()?
            .ToArray() ?? [];
        var duplicates = registrations
            .GroupBy(static registration => registration.Descriptor.BackendId)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .OrderBy(static backendId => backendId.Value, StringComparer.Ordinal)
            .ToArray();

        if (duplicates.Length != 0)
        {
            Thrower.InvalidOpEx(
                "Multiple runtime registrations were found for backend id(s): " +
                string.Join(", ", duplicates.Select(static backendId => backendId.Value)) + ".");
        }

        return registrations.ToDictionary(static registration => registration.Descriptor.BackendId);
    }

}
