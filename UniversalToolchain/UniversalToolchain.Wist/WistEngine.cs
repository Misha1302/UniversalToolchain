using ExceptionsManager;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Wist;

/// <summary>
///     Public Wist facade for convenient evaluation and typed fast compiled functions.
/// </summary>
public sealed class WistEngine : IDisposable
{
    private readonly WistDialectExecutionHost _host;
    private readonly WistEngineOptions _options;
    private bool _disposed;

    private WistEngine(WistDialectExecutionHost host, WistEngineOptions options)
    {
        _host = host;
        _options = options;
    }

    /// <summary>
    ///     Releases the composed Wist runtime.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _host.Dispose();
        _disposed = true;
    }

    /// <summary>
    ///     Creates a safe formula-oriented Wist engine.
    /// </summary>
    public static WistEngine CreateSafeFormulas() => Create(new WistEngineOptions { Preset = WistPreset.SafeFormulas });

    /// <summary>
    ///     Creates a business-rule oriented Wist engine.
    /// </summary>
    public static WistEngine CreateBusinessRules() => Create(new WistEngineOptions { Preset = WistPreset.BusinessRules });

    /// <summary>
    ///     Creates a full trusted Wist engine. Do not use this for untrusted input.
    /// </summary>
    public static WistEngine CreateTrusted() => Create(new WistEngineOptions { Preset = WistPreset.FullTrusted });

    /// <summary>
    ///     Creates a Wist engine from public facade options.
    /// </summary>
    public static WistEngine Create(WistEngineOptions options)
    {
        options = options.ArgNotNull();

        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(
            new WistShippedDialectFileResolver()
                .Resolve(WistPresetMapper.ToShippedPreset(options.Preset)));

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return new WistEngine(workflow.CreateHost(composition), options);
    }

    /// <summary>
    ///     Evaluates source text through the configured convenience backend.
    /// </summary>
    public T Evaluate<T>(string code) => WistResultConverter.ConvertTo<T>(_host.Run(code, WistBackendAliases.ToAlias(_options.Backend)));

    /// <summary>
    ///     Evaluates source text with anonymous-object or dictionary arguments.
    /// </summary>
    public T Evaluate<T>(string code, object arguments) => Evaluate<T>(code, WistArgumentReader.FromObject(arguments));

    /// <summary>
    ///     Evaluates source text with named arguments through the configured convenience backend.
    /// </summary>
    public T Evaluate<T>(string code, IReadOnlyDictionary<string, object?> arguments) => WistResultConverter.ConvertTo<T>(_host.Run(code, arguments, WistBackendAliases.ToAlias(_options.Backend)));

    /// <summary>
    ///     Validates source text by attempting to compile it for the configured backend.
    /// </summary>
    public WistValidationResult Validate(string code)
    {
        try
        {
            _ = _host.Compile(code, null, WistBackendAliases.ToAlias(_options.Backend));
            return WistValidationResult.Success();
        }
        catch (Exception ex)
        {
            return WistValidationResult.Failure(ex);
        }
    }

    /// <summary>
    ///     Validates source text with sample arguments by attempting to compile it for the configured backend.
    /// </summary>
    public WistValidationResult Validate(string code, object sampleArguments)
    {
        try
        {
            var declaredBindings = CreateDeclaredBindings(WistArgumentReader.FromObject(sampleArguments));
            _ = _host.Compile(code, declaredBindings, WistBackendAliases.ToAlias(_options.Backend));
            return WistValidationResult.Success();
        }
        catch (Exception ex)
        {
            return WistValidationResult.Failure(ex);
        }
    }

    /// <summary>
    ///     Compiles a one-argument CIL-backed typed fast function.
    /// </summary>
    public WistFunc<TArg0, TResult> CompileFunc<TArg0, TResult>(string formula, string arg0)
    {
        var dynamicMethod = CompileDynamicMethod(
            formula,
            WistArgumentReader.TypesFromNamesAndTypes((arg0, typeof(TArg0))));

        return new WistFunc<TArg0, TResult>(dynamicMethod);
    }

    /// <summary>
    ///     Compiles a two-argument CIL-backed typed fast function.
    /// </summary>
    public WistFunc<TArg0, TArg1, TResult> CompileFunc<TArg0, TArg1, TResult>(string formula, string arg0, string arg1)
    {
        var dynamicMethod = CompileDynamicMethod(
            formula,
            WistArgumentReader.TypesFromNamesAndTypes((arg0, typeof(TArg0)), (arg1, typeof(TArg1))));

        return new WistFunc<TArg0, TArg1, TResult>(dynamicMethod);
    }

    /// <summary>
    ///     Compiles a three-argument CIL-backed typed fast function.
    /// </summary>
    public WistFunc<TArg0, TArg1, TArg2, TResult> CompileFunc<TArg0, TArg1, TArg2, TResult>(string formula, string arg0, string arg1, string arg2)
    {
        var dynamicMethod = CompileDynamicMethod(
            formula,
            WistArgumentReader.TypesFromNamesAndTypes((arg0, typeof(TArg0)), (arg1, typeof(TArg1)), (arg2, typeof(TArg2))));

        return new WistFunc<TArg0, TArg1, TArg2, TResult>(dynamicMethod);
    }

    private DynamicMethod CompileDynamicMethod(string formula, IReadOnlyDictionary<string, Type> bindingTypes)
    {
        var artifact = _host.GetBackendSpecificArtifactCompiler<BasicCilCompiler.Execution.CilCompilationOutput>(WistBackendAliases.CompilerAlias).Compile(formula, CreateDeclaredBindings(bindingTypes));
        return artifact.CompilationOutput.Method;
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings(IReadOnlyDictionary<string, object?> arguments)
    {
        var bindings = new OrderedDictionary<string, Type>();
        foreach (var argument in arguments)
            bindings[argument.Key] = argument.Value?.GetType() ?? typeof(object);

        return bindings;
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings(IReadOnlyDictionary<string, Type> bindingTypes)
    {
        var bindings = new OrderedDictionary<string, Type>();
        foreach (var binding in bindingTypes)
            bindings[binding.Key] = binding.Value;

        return bindings;
    }
}
