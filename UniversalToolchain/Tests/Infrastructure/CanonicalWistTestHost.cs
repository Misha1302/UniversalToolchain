using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace Tests.Infrastructure;

internal sealed class CanonicalWistTestHost : IDisposable
{
    private readonly WistLanguageFeaturePackage _package;
    private readonly LanguageRuntime _runtime;

    public CanonicalWistTestHost()
        : this(WistLanguageDefinitions.Create(WistLanguageDefinitions.FullDefaultNativeId), [])
    {
    }

    public CanonicalWistTestHost(
        string dialectText,
        string backendName,
        IReadOnlyList<Assembly>? allowedAssemblies = null)
        : this(
            WistFacadeLanguageDefinitionFactory.FromDialectText(
                dialectText,
                "canonical-test-inline",
                RequireBackend(backendName).Value,
                WistFacadeSsaPolicy.Disabled),
            allowedAssemblies ?? [])
    {
    }

    private CanonicalWistTestHost(
        LanguageDefinition definition,
        IReadOnlyList<Assembly> allowedAssemblies)
    {
        _package = new WistLanguageFeaturePackage();
        Plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(_package))
            .Compile(definition)
            .GetRequiredPlan();
        var runtimeAssemblies = Plan.Definition.RuntimePolicy.AllowHostInterop
            ? allowedAssemblies.ToArray()
            : [];
        _runtime = LanguageRuntime.Create(
            Plan,
            new ILanguageRouteComponentSource[] { _package },
            new LanguageRuntimeOptions(runtimeAssemblies));
    }

    public LanguagePlan Plan { get; }

    public CanonicalWistBuiltProgram Compile(
        string code,
        OrderedDictionary<string, Type> declared,
        string backendName)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(declared);
        var backend = RequireBackend(backendName);
        var bindings = declared.Select(binding =>
            LanguageBuildBinding.Declare(
                binding.Key,
                WistRuntimeValueAdapterActivation.NormalizeDeclaredType(Plan, binding.Value)))
            .ToArray();
        var built = _runtime.Build(LanguageArtifactBuildRequest.FromText(code, backend, bindings));
        var program = WistBuiltArtifactActivation.Materialize(_runtime, built);
        return new CanonicalWistBuiltProgram(built, program);
    }

    public WistCilArtifact GetCilArtifact(CanonicalWistBuiltProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (program.BuildResult.Backend.Value != "cil")
            throw new InvalidOperationException("Built program is not a CIL artifact.");
        return _runtime.GetBuiltArtifactValue(program.BuildResult, WistDirectBackendArtifactKinds.Cil);
    }

    public object? Run(
        string code,
        string backendName,
        IReadOnlyDictionary<string, object?>? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        var normalized = (arguments ?? new Dictionary<string, object?>())
            .ToDictionary(
                static argument => argument.Key,
                argument => WistRuntimeValueAdapterActivation.NormalizeInput(Plan, argument.Value),
                StringComparer.Ordinal);
        var result = _runtime.Run(new LanguageExecutionRequest(code, RequireBackend(backendName), normalized));
        return WistRuntimeValueAdapterActivation.Normalize(Plan, result.Value);
    }

    public object? Run(
        CanonicalWistBuiltProgram program,
        IReadOnlyDictionary<string, object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(arguments);
        var ordered = program.DeclaredBindings
            .Select(binding => arguments.TryGetValue(binding.Name, out var value)
                ? value
                : throw new ArgumentException($"Missing runtime argument '{binding.Name}'.", nameof(arguments)))
            .ToArray();
        return program.Program.Invoke(ordered);
    }

    public void Dispose() => _runtime.Dispose();

    private static BackendId RequireBackend(string backendName) => backendName switch
    {
        "cil" => new BackendId("cil"),
        "interpreter" => new BackendId("interpreter"),
        _ => throw new ArgumentOutOfRangeException(nameof(backendName), backendName, "Expected 'cil' or 'interpreter'.")
    };
}

internal sealed class CanonicalWistBuiltProgram
{
    public CanonicalWistBuiltProgram(
        LanguageArtifactBuildResult buildResult,
        IWistDurableProgram program)
    {
        BuildResult = buildResult ?? throw new ArgumentNullException(nameof(buildResult));
        Program = program ?? throw new ArgumentNullException(nameof(program));
        DeclaredBindings = program.DeclaredBindings;
        SlotsByName = DeclaredBindings
            .Select((binding, index) => new KeyValuePair<string, int>(binding.Name, index))
            .ToDictionary(static item => item.Key, static item => item.Value, StringComparer.Ordinal);
    }

    public LanguageArtifactBuildResult BuildResult { get; }
    public IWistDurableProgram Program { get; }
    public IReadOnlyList<ExternalBinding> DeclaredBindings { get; }
    public IReadOnlyDictionary<string, int> SlotsByName { get; }
}
