using System.Collections.ObjectModel;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

public sealed class LanguageBuildBinding
{
    public LanguageBuildBinding(string name, Type valueType, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Build binding name must not be empty.", nameof(name));
        Name = name;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        ValidateValue(valueType, value, nameof(value));
        Value = value;
    }

    public string Name { get; }
    public Type ValueType { get; }
    public object? Value { get; }

    public static LanguageBuildBinding Create<T>(string name, T value) => new(name, typeof(T), value);

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
        var duplicate = snapshot.GroupBy(static binding => binding.Name, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate != null)
            throw new ArgumentException($"Build binding '{duplicate.Key}' is declared more than once.", nameof(bindings));
        Bindings = new ReadOnlyCollection<LanguageBuildBinding>(snapshot);
        Arguments = new ReadOnlyDictionary<string, object?>(
            snapshot.ToDictionary(static binding => binding.Name, static binding => binding.Value, StringComparer.Ordinal));
    }

    public LanguageArtifact Input { get; }
    public BackendId Backend { get; }
    public IReadOnlyList<LanguageBuildBinding> Bindings { get; }
    public IReadOnlyDictionary<string, object?> Arguments { get; }

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

    internal LanguageExecutionRequest ToExecutionRequest() => new(Input, Backend, Arguments);
}

public sealed record LanguageArtifactBuildStep(
    LanguageContributionId ContributionId,
    LanguageArtifactContract SourceContract,
    LanguageArtifactContract TargetContract);

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
