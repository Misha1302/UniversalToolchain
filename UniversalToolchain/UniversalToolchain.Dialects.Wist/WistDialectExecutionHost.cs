using BasicCore.Compilation;
using BasicCore.Contracts;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Provides backend-aware access to a dialect-configured Wist runtime.
/// </summary>
public sealed class WistDialectExecutionHost : IDisposable
{
    private readonly ToolchainRuntimeHost _runtimeHost;

    public WistDialectExecutionHost(IServiceProvider serviceProvider, WistDialectExecutionConfiguration configuration)
        : this(new ToolchainRuntimeHost(serviceProvider.ArgNotNull(), configuration.ArgNotNull()), configuration)
    {
    }

    internal WistDialectExecutionHost(ToolchainRuntimeHost runtimeHost, WistDialectExecutionConfiguration configuration)
    {
        runtimeHost = runtimeHost.ArgNotNull();
        configuration = configuration.ArgNotNull();

        Configuration = configuration;
        _runtimeHost = runtimeHost;
    }

    public WistDialectExecutionConfiguration Configuration { get; }

    public void Dispose()
    {
        _runtimeHost.Dispose();
    }

    public ICoreRunnable GetCore(string backend)
        => _runtimeHost.GetCore(backend);

    public IArtifactCompiler GetArtifactCompiler(string backend)
        => _runtimeHost.GetArtifactCompiler(backend);

    /// <summary>
    ///     Gets a typed backend-specific artifact compiler for tests, benchmarks, and explicit fast-path adapters.
    /// </summary>
    public IArtifactCompiler<TCompilationOutput> GetBackendSpecificArtifactCompiler<TCompilationOutput>(string backend)
    {
        return _runtimeHost.GetBackendSpecificArtifactCompiler<TCompilationOutput>(backend);
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
        string backend)
    {
        code = code.ArgNotNull();
        arguments = arguments.ArgNotNull();

        var declaredBindings = WistDeclaredBindingFactory.FromRuntimeArguments(arguments);
        return _runtimeHost.Run(code, arguments, declaredBindings, backend);
    }

    public object? Run(string code, string backend) => _runtimeHost.Run(code, backend);
}
