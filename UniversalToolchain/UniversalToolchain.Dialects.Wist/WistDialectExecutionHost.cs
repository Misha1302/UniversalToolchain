using BasicCore.Compilation;
using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Provides backend-aware access to a dialect-configured Wist runtime.
/// </summary>
public sealed class WistDialectExecutionHost : IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    public WistDialectExecutionHost(IServiceProvider serviceProvider, WistDialectExecutionConfiguration configuration)
    {
        serviceProvider = serviceProvider.ArgNotNull();
        configuration = configuration.ArgNotNull();

        _serviceProvider = serviceProvider;
        Configuration = configuration;
    }

    public WistDialectExecutionConfiguration Configuration { get; }

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

    [Obsolete("Use backend-neutral GetArtifactCompiler(string), Compile(...), or Run(...) unless typed backend internals are the purpose of the caller.")]
    public IArtifactCompiler<TCompilationOutput> GetArtifactCompiler<TCompilationOutput>(string backend)
        => GetBackendSpecificArtifactCompiler<TCompilationOutput>(backend);

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
        string backend)
    {
        code = code.ArgNotNull();
        arguments = arguments.ArgNotNull();

        var declaredBindings = WistDeclaredBindingFactory.FromRuntimeArguments(arguments);
        var artifact = Compile(code, declaredBindings, backend);
        var session = artifact.CreateSession();

        foreach (var argument in arguments)
            session.SetArgument(argument.Key, argument.Value);

        return session.Run();
    }

    public object? Run(string code, string backend) => GetCore(backend).Run(code);

    private WistDialectBackendRuntime ResolveRuntime(string backend)
    {
        if (string.IsNullOrWhiteSpace(backend))
            Thrower.Argument(nameof(backend), "Backend name must not be empty.");

        if (!Configuration.TryResolveKnownBackendId(backend, out var backendId))
        {
            var supportedBackends = string.Join(
                ", ",
                Configuration.EnabledBackends
                    .SelectMany(x => x.AllNames)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal));
            Thrower.InvalidOpEx($"Unknown backend '{backend}'. Supported backends: {supportedBackends}.");
        }

        if (!Configuration.TryGetEnabledBackend(backendId, out var backendConfiguration))
            Thrower.InvalidOpEx($"Dialect '{Configuration.DialectName}' does not enable backend '{backend}'.");

        var runtime = _serviceProvider.GetServices<WistDialectBackendRuntime>()
            .FirstOrDefault(x => x.Descriptor.BackendId == backendConfiguration.BackendDescriptor.BackendId);
        if (runtime == null)
            Thrower.InvalidOpEx<WistDialectBackendRuntime>($"Backend core '{backendConfiguration.BackendDescriptor.CanonicalId}' was not registered.");

        return runtime;
    }
}
