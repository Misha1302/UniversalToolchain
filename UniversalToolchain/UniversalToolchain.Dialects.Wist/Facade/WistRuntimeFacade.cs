using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist.Facade;

/// <summary>
///     Provides a small Wist-specific programmatic entry point over the dialect execution host.
/// </summary>
internal sealed class WistRuntimeFacade : IDisposable
{
    private readonly WistDialectExecutionHost _host;

    internal WistRuntimeFacade(WistDialectExecutionHost host, DialectFrameworkCompositionResult composition)
    {
        _host = host.ArgNotNull();
        Composition = composition.ArgNotNull();
    }

    internal ToolchainRuntimeConfiguration Configuration => _host.Configuration;

    internal DialectFrameworkCompositionResult Composition { get; }

    public void Dispose() => _host.Dispose();

    /// <summary>
    ///     Executes Wist source text with named arguments through the selected backend.
    /// </summary>
    public object? Run(
        string code,
        IReadOnlyDictionary<string, object?> arguments,
        string backend = "cil")
        => Run(new WistRunRequest(code, arguments, backend));

    /// <summary>
    ///     Executes a prepared Wist facade request.
    /// </summary>
    public object? Run(WistRunRequest request)
    {
        request = request.ArgNotNull();

        return WistRuntimeValueNormalizer.Normalize(
            _host.Run(
                request.Code,
                request.Arguments,
                request.Backend));
    }

    /// <summary>
    ///     Attempts to compile Wist source text with the selected backend and captures any failure.
    /// </summary>
    public WistTryCompileResult TryCompile(
        string code,
        IReadOnlyDictionary<string, Type> declaredBindings,
        string backend = "cil")
    {
        try
        {
            var artifact = _host.Compile(
                code,
                WistDeclaredBindingFactory.FromDeclaredTypes(declaredBindings),
                backend);

            return WistTryCompileResult.Success(artifact);
        }
        catch (Exception ex)
        {
            return WistTryCompileResult.Failure(ex);
        }
    }
}