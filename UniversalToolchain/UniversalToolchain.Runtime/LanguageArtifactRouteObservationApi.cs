using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

/// <summary>
/// Public, language-neutral observation emitted after a planned artifact-route transformation completes.
/// It exposes only the immutable plan/route state required by diagnostics and contract verification;
/// observers cannot influence planning, component discovery, or backend selection.
/// </summary>
public sealed record LanguageArtifactRouteObservationContext(
    LanguagePlan Plan,
    BackendId Backend,
    IReadOnlyList<LanguageArtifactRouteStep> RouteSteps,
    int StepIndex,
    LanguageArtifactRouteStep Step,
    LanguageArtifact Artifact);

/// <summary>
/// Language-neutral hook for observing transformations that were already selected by <see cref="LanguagePlan"/>.
/// Implementations are observational only: they receive the resulting artifact after each transformation.
/// </summary>
public interface ILanguageArtifactRouteListener
{
    void AfterTransformation(LanguageArtifactRouteObservationContext observation);
}

public static class LanguageRuntimeOptionsRouteObservationExtensions
{
    /// <summary>
    /// Adds an observational listener without exposing Runtime's internal dispatcher contract.
    /// </summary>
    public static LanguageRuntimeOptions AddRouteListener(
        this LanguageRuntimeOptions options,
        ILanguageArtifactRouteListener listener)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(listener);
        options.AddRouteObserver(new ListenerAdapter(listener));
        return options;
    }

    private sealed class ListenerAdapter(ILanguageArtifactRouteListener listener) : ILanguageArtifactRouteObserver
    {
        private readonly ILanguageArtifactRouteListener _listener =
            listener ?? throw new ArgumentNullException(nameof(listener));

        public void AfterTransformation(LanguageArtifactRouteObservation observation)
        {
            ArgumentNullException.ThrowIfNull(observation);
            _listener.AfterTransformation(new LanguageArtifactRouteObservationContext(
                observation.Plan,
                observation.Backend,
                observation.RouteSteps,
                observation.StepIndex,
                observation.Step,
                observation.Artifact));
        }
    }
}
