using System.Reflection.Emit;
using BasicCore.Compilation;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist.Rules;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Facade;

/// <summary>
///     Provides a small Wist-specific programmatic entry point over the dialect execution host.
/// </summary>
public sealed class WistRuntimeFacade : IDisposable
{
    private readonly WistDialectExecutionHost _host;

    internal WistRuntimeFacade(WistDialectExecutionHost host, DialectFrameworkCompositionResult composition)
    {
        _host = host.ArgNotNull();
        Composition = composition.ArgNotNull();
    }

    internal WistDialectExecutionConfiguration Configuration => _host.Configuration;

    internal DialectFrameworkCompositionResult Composition { get; }

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
    ///     Compiles Wist rule declarations through the existing Wist runtime pipeline.
    /// </summary>
    public RuleSetCompileResult CompileRuleSet(string source, string mode = "compiler")
    {
        var extraction = new WistRuleDeclarationExtractor().Extract(source);
        if (!extraction.IsSuccess)
            return new RuleSetCompileResult(false, null, extraction.Diagnostics);

        var diagnostics = new List<ToolchainDiagnostic>();
        var compiledRules = new List<ICompiledRule>();

        foreach (var rule in extraction.Rules.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            var declaredBindings = CreateDeclaredBindings(rule.Parameters);
            try
            {
                var artifact = Compile(rule.Body, declaredBindings, mode);
                compiledRules.Add(new CompiledWistRule(CreateDescriptor(rule), artifact));
            }
            catch (Exception ex)
            {
                diagnostics.Add(new ToolchainDiagnostic(
                    ToolchainDiagnosticCodes.RuleInvalidBody,
                    ToolchainDiagnosticSeverity.Error,
                    $"Rule '{rule.Name}' could not be compiled: {ex.Message}",
                    null,
                    []));
            }
        }

        return diagnostics.Count == 0
            ? RuleSetCompileResult.Success(new CompiledWistRuleSet(compiledRules))
            : RuleSetCompileResult.Failure(diagnostics);
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

    private static OrderedDictionary<string, Type> CreateDeclaredBindings(IReadOnlyList<RuleParameterModel> parameters)
    {
        parameters = parameters.ArgNotNull();

        var declaredBindings = new OrderedDictionary<string, Type>();
        foreach (var parameter in parameters)
            declaredBindings[parameter.Name] = ResolveRuntimeType(parameter.Type);

        return declaredBindings;
    }

    private static CompiledRuleDescriptor CreateDescriptor(RuleDeclarationModel rule)
    {
        return new CompiledRuleDescriptor(
            rule.Name,
            rule.Parameters
                .Select(static x => new RuleParameterDescriptor(x.Name, x.Type))
                .ToList(),
            rule.ReturnType);
    }

    private static Type ResolveRuntimeType(RuleTypeDescriptor type)
    {
        return type.Name switch
        {
            "number" => typeof(double),
            "bool" => typeof(bool),
            _ => Thrower.NotSupported<Type>($"Unsupported rule type '{type.Name}'.")
        };
    }
}
