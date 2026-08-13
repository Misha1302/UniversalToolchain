using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

/// <summary>
/// A language runtime that can both execute a planned language and materialize exact planned artifacts.
/// Instances are created only from exact <see cref="ILanguageRouteComponentSource"/> registrations.
/// </summary>
public sealed class LanguageBuildRuntime : LanguageRuntime
{
    private readonly ILanguageArtifactBuildSession _buildSession;

    internal LanguageBuildRuntime(
        LanguagePlan plan,
        ILanguageRuntimeSession session,
        ILanguageArtifactBuildSession buildSession)
        : base(plan, session)
    {
        _buildSession = buildSession ?? throw new ArgumentNullException(nameof(buildSession));
    }

    public LanguageArtifactBuildResult Build(LanguageArtifactBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var operation = EnterRuntimeOperation();
        ValidateOperationInput(request.Input, request.Backend, request.Bindings.Count, "Build");
        return _buildSession.Build(request);
    }

    public LanguageExecutionResult ExecuteBuilt(LanguageArtifactBuildResult artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        using var operation = EnterRuntimeOperation();
        return _buildSession.ExecuteBuilt(artifact);
    }

    public T GetBuiltArtifactValue<T>(LanguageArtifactBuildResult artifact, LanguageArtifactKind<T> expectedKind)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(expectedKind);
        using var operation = EnterRuntimeOperation();
        return _buildSession.GetBuiltArtifactValue(artifact, expectedKind);
    }

    private protected override void DisposeCore()
    {
        var errors = RuntimeConstructionFailure.DisposeSynchronouslyCollect(SessionOwner, _buildSession);
        if (errors.Count != 0)
            throw new AggregateException("One or more language build runtime owners failed to dispose.", errors);
    }

    private protected override async ValueTask DisposeAsyncCore()
    {
        var errors = await RuntimeConstructionFailure.DisposeAsynchronouslyCollect(SessionOwner, _buildSession)
            .ConfigureAwait(false);
        if (errors.Count != 0)
            throw new AggregateException("One or more language build runtime owners failed to dispose.", errors);
    }
}
