using System.Reflection.Emit;
using BasicCore.Compilation;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Provides a small Wist-specific programmatic entry point over the dialect execution host.
/// </summary>
public sealed class WistRuntimeFacade : IDisposable
{
    private readonly WistDialectExecutionHost _host;

    internal WistRuntimeFacade(WistDialectExecutionHost host)
    {
        _host = host.ArgNotNull();
    }

    public void Dispose() => _host.Dispose();

    /// <summary>
    ///     Executes Wist source text with named arguments through the selected backend.
    /// </summary>
    public object? Run(
        string code,
        IReadOnlyDictionary<string, object?> arguments,
        string mode = "compiler")
        => Run(new WistRunRequest(code, arguments, mode));

    /// <summary>
    ///     Executes a prepared Wist facade request.
    /// </summary>
    public object? Run(WistRunRequest request)
    {
        request = request.ArgNotNull();

        var declaredBindings = CreateDeclaredBindings(request.Arguments);
        var artifact = Compile(request.Code, declaredBindings, request.Mode);
        var session = artifact.CreateSession();

        foreach (var argument in request.Arguments)
            session.SetArgument(argument.Key, argument.Value);

        return session.Run();
    }

    /// <summary>
    ///     Attempts to compile Wist source text with the selected backend and captures any failure.
    /// </summary>
    public WistTryCompileResult TryCompile(
        string code,
        IReadOnlyDictionary<string, Type> declaredBindings,
        string mode = "compiler")
    {
        try
        {
            var artifact = Compile(code, CreateDeclaredBindings(declaredBindings), mode);
            return WistTryCompileResult.Success(artifact);
        }
        catch (Exception ex)
        {
            return WistTryCompileResult.Failure(ex);
        }
    }

    private ICompiledArtifact Compile(string code, OrderedDictionary<string, Type> declaredBindings, string mode)
    {
        var backendId = ResolveBackendId(mode);

        if (backendId == WistDialectBackendIds.Interpreter)
            return _host.GetArtifactCompiler<IAbstractIR>(mode).Compile(code, declaredBindings);

        if (backendId == WistDialectBackendIds.Cil)
            return _host.GetArtifactCompiler<DynamicMethod>(mode).Compile(code, declaredBindings);

        return Thrower.InvalidOpEx<ICompiledArtifact>($"Unsupported Wist facade backend '{mode}'.");
    }

    private DialectBackendId ResolveBackendId(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            Thrower.Argument(nameof(mode), "Execution mode must not be empty.");

        if (!_host.Configuration.TryResolveKnownBackendId(mode, out var backendId))
            Thrower.InvalidOpEx($"Unknown execution mode '{mode}'.");

        return backendId;
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings(IReadOnlyDictionary<string, object?> arguments)
    {
        arguments = arguments.ArgNotNull();

        var declaredBindings = new OrderedDictionary<string, Type>();
        foreach (var argument in arguments)
        {
            if (string.IsNullOrWhiteSpace(argument.Key))
                Thrower.Argument(nameof(arguments), "Argument names must not be empty.");

            declaredBindings[argument.Key] = argument.Value?.GetType() ?? typeof(object);
        }

        return declaredBindings;
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings(IReadOnlyDictionary<string, Type> bindings)
    {
        bindings = bindings.ArgNotNull();

        var declaredBindings = new OrderedDictionary<string, Type>();
        foreach (var binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Key))
                Thrower.Argument(nameof(bindings), "Binding names must not be empty.");

            declaredBindings[binding.Key] = binding.Value.ArgNotNull();
        }

        return declaredBindings;
    }
}