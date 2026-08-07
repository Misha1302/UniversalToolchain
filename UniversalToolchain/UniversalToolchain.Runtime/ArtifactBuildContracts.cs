using System.Collections.ObjectModel;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

/// <summary>
/// Compile-time binding declaration used by build-only artifact construction. A binding may carry
/// an optional sample/runtime value, but its declared CLR type is always explicit and is never
/// inferred from that value.
/// </summary>
public sealed class LanguageBuildBinding
{
    private LanguageBuildBinding(string name, Type valueType, bool hasValue, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Build binding name must not be empty.", nameof(name));
        Name = name;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        HasValue = hasValue;
        if (hasValue)
            ValidateValue(valueType, value, nameof(value));
        Value = value;
    }

    public string Name { get; }
    public Type ValueType { get; }
    public bool HasValue { get; }
    public object? Value { get; }

    public static LanguageBuildBinding Declare<T>(string name) => new(name, typeof(T), false, null);

    public static LanguageBuildBinding Declare(string name, Type valueType) =>
        new(name, valueType, false, null);

    public static LanguageBuildBinding Create<T>(string name, T value) =>
        new(name, typeof(T), true, value);

    public static LanguageBuildBinding Create(string name, Type valueType, object? value) =>
        new(name, valueType, true, value);

    private static void ValidateValue(Type valueType, object? value, string parameterName)
    {
        if (value == null)
        {
            if (valueType.IsValueType && Nullable.GetUnderlyingType(valueType) == null)
            {
                throw new ArgumentException(
                    $"Null cannot satisfy non-nullable build binding type '{valueType.FullName}'.",
                    parameterName);
            }
            return;
        }
        if (!valueType.IsInstanceOfType(value))
        {
            throw new ArgumentException(
                $"Build binding value type '{value.GetType().FullName}' does not satisfy declared type '{valueType.FullName}'.",
                parameterName);
        }
    }
}

public sealed class LanguageArtifactBuildRequest
{
    public LanguageArtifactBuildRequest(
        LanguageArtifact input,
        BackendId backend,
        IEnumerable<LanguageBuildBinding>? bindings = null)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Backend = backend;
        var snapshot = (bindings ?? []).ToArray();
        if (snapshot.Any(static binding => binding == null))
            throw new ArgumentException("Build bindings must not contain null entries.", nameof(bindings));
        var duplicate = snapshot.GroupBy(static binding => binding.Name, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate != null)
            throw new ArgumentException($"Build binding '{duplicate.Key}' is declared more than once.", nameof(bindings));
        Bindings = new ReadOnlyCollection<LanguageBuildBinding>(snapshot);
        DeclaredBindingTypes = new ReadOnlyDictionary<string, Type>(
            snapshot.ToDictionary(static binding => binding.Name, static binding => binding.ValueType, StringComparer.Ordinal));
        RuntimeArguments = new ReadOnlyDictionary<string, object?>(
            snapshot.Where(static binding => binding.HasValue)
                .ToDictionary(static binding => binding.Name, static binding => binding.Value, StringComparer.Ordinal));
    }

    public LanguageArtifact Input { get; }
    public BackendId Backend { get; }
    public IReadOnlyList<LanguageBuildBinding> Bindings { get; }
    public IReadOnlyDictionary<string, Type> DeclaredBindingTypes { get; }
    public IReadOnlyDictionary<string, object?> RuntimeArguments { get; }

    public static LanguageArtifactBuildRequest FromText(
        string input,
        BackendId backend,
        IEnumerable<LanguageBuildBinding>? bindings = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new LanguageArtifactBuildRequest(
            new LanguageArtifact<string>(StandardLanguageArtifactKinds.SourceText, input),
            backend,
            bindings);
    }

    internal LanguageExecutionRequest ToExecutionRequest() => new(Input, Backend, RuntimeArguments);
}

/// <summary>
/// Context available only to build-aware transformers. It carries explicit compile-time bindings
/// separately from runtime argument values; arbitrary metadata strings are not a semantic channel.
/// </summary>
public sealed class LanguageArtifactBuildContext
{
    internal LanguageArtifactBuildContext(
        LanguagePlan plan,
        LanguageArtifactBuildRequest request,
        LanguageRuntimeOptions options,
        LanguageExecutionRequest executionRequest)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ExecutionRequest = executionRequest ?? throw new ArgumentNullException(nameof(executionRequest));
        TransformationContext = new LanguageArtifactTransformationContext(plan, executionRequest, options);
    }

    public LanguagePlan Plan { get; }
    public LanguageArtifactBuildRequest Request { get; }
    public LanguageRuntimeOptions Options { get; }
    public LanguageArtifactTransformationContext TransformationContext { get; }
    internal LanguageExecutionRequest ExecutionRequest { get; }
}

/// <summary>
/// Optional transformer contract for stages whose compile-time behavior depends on declared
/// bindings. Stages that do not implement it receive the ordinary transformation context.
/// </summary>
public interface ILanguageArtifactBuildTransformer
{
    LanguageArtifact TransformForBuild(LanguageArtifact source, LanguageArtifactBuildContext context);
}

public sealed record LanguageArtifactBuildStep(
    LanguageContributionId ContributionId,
    LanguageArtifactContract SourceContract,
    LanguageArtifactContract TargetContract);

public enum LanguageBuiltArtifactLifetime
{
    OriginatingRuntime
}

public sealed class LanguageArtifactBuildResult
{
    internal LanguageArtifactBuildResult(
        LanguageId languageId,
        LanguageVersion languageVersion,
        string planHash,
        BackendId backend,
        LanguageArtifact artifact,
        IReadOnlyList<LanguageArtifactBuildStep> steps,
        IReadOnlyList<LanguageDiagnostic> diagnostics,
        LanguageExecutionRequest executionRequest,
        object ownerToken)
    {
        if (string.IsNullOrWhiteSpace(planHash))
            throw new ArgumentException("Plan hash must not be empty.", nameof(planHash));
        LanguageId = languageId;
        LanguageVersion = languageVersion;
        PlanHash = planHash;
        Backend = backend;
        Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
        ArtifactContract = artifact.Contract;
        Steps = new ReadOnlyCollection<LanguageArtifactBuildStep>((steps ?? throw new ArgumentNullException(nameof(steps))).ToArray());
        Diagnostics = new ReadOnlyCollection<LanguageDiagnostic>((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
        ExecutionRequest = executionRequest ?? throw new ArgumentNullException(nameof(executionRequest));
        OwnerToken = ownerToken ?? throw new ArgumentNullException(nameof(ownerToken));
    }

    public LanguageId LanguageId { get; }
    public LanguageVersion LanguageVersion { get; }
    public string PlanHash { get; }
    public BackendId Backend { get; }
    public LanguageArtifactContract ArtifactContract { get; }
    public LanguageBuiltArtifactLifetime Lifetime => LanguageBuiltArtifactLifetime.OriginatingRuntime;
    public IReadOnlyList<LanguageArtifactBuildStep> Steps { get; }
    public IReadOnlyList<LanguageDiagnostic> Diagnostics { get; }

    internal LanguageArtifact Artifact { get; }
    internal LanguageExecutionRequest ExecutionRequest { get; }
    internal object OwnerToken { get; }
}

public interface ILanguageArtifactBuildSession
{
    LanguageArtifactBuildResult Build(LanguageArtifactBuildRequest request);
    LanguageExecutionResult ExecuteBuilt(LanguageArtifactBuildResult artifact);
    T GetBuiltArtifactValue<T>(LanguageArtifactBuildResult artifact, LanguageArtifactKind<T> expectedKind);
}
