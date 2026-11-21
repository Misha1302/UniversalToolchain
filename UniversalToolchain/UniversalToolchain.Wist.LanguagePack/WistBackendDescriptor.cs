using BasicCore.Capabilities;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Runtime;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Everything the Wist pack needs to know about one backend, declared once instead of inferred from a name.
/// <para>
/// Before this type, backend identity was a string compared against literals in five places, three of
/// which ended in <c>throw new InvalidOperationException("Unsupported Wist backend '...'")</c>. Adding a
/// third backend meant finding and editing every one of them, and the compiler could not help: the
/// default arm of a string switch is not an error until it runs.
/// </para>
/// <para>
/// This is a <b>description</b>, not a selection authority. It says what a backend can do and how to
/// project its artifacts; it never decides which backend a plan uses. Selection stays with
/// <c>LanguageCompiler</c>. The descriptor exists so the pack can ask about a backend it did not name.
/// </para>
/// <para>
/// Two members are deliberately <b>not</b> declared here: the contract-descriptor provider type (each
/// backend's provider is a distinct unrelated class, so a factory delegate is the honest shape) and the
/// pipeline component id, which the pack derives from the contribution.
/// </para>
/// </summary>
internal sealed record WistBackendDescriptor(
    BackendId Id,
    IReadOnlyList<string> SupportedIntrinsics,
    string PipelineComponentId,
    Func<IReadOnlyList<string>, object> CreateContractDescriptorProvider,
    Func<IIntrinsicCapabilitySet> CreateIntrinsicCapabilitySet,
    Func<LanguageBuildRuntime, LanguageArtifactBuildResult, IWistDurableProgram> MaterializeProgram,
    Func<LanguageBuildRuntime, LanguageArtifactBuildResult, SsaRouteReport?> ProjectSsaReport);

/// <summary>
/// The pack's registry of backend descriptors.
/// <para>
/// Lookup by <see cref="BackendId"/> and nothing else. A name the catalog does not describe is a real
/// error, and the message names the backend and lists what is described, so an author adding a third
/// backend sees what to add rather than a bare <c>throw</c>.
/// </para>
/// <para>
/// This is an instance, not a static registry, for one reason: the claim "a third backend needs no edit
/// to the consumers" has to be testable. A test can build a catalog that contains a backend the pack does
/// not ship and observe that lookup, composition and rejection all work without a name being compared.
/// A mutable static would have allowed the same test by mutating global state, which is worse.
/// </para>
/// </summary>
internal sealed class WistBackendCatalog
{
    private static readonly Lazy<WistBackendCatalog> CanonicalInstance = new(
        () => new WistBackendCatalog(WistBackends.Descriptors));

    private readonly IReadOnlyDictionary<BackendId, WistBackendDescriptor> _byId;

    public WistBackendCatalog(IEnumerable<WistBackendDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var list = descriptors.ToArray();
        _byId = list.ToDictionary(static descriptor => descriptor.Id);
        All = list.OrderBy(static descriptor => descriptor.Id.Value, StringComparer.Ordinal).ToArray();
    }

    /// <summary>The catalog of backends the pack actually ships.</summary>
    public static WistBackendCatalog Canonical { get; } = CanonicalInstance.Value;

    public IReadOnlyList<WistBackendDescriptor> All { get; }

    public bool TryGet(BackendId backend, out WistBackendDescriptor descriptor) =>
        _byId.TryGetValue(backend, out descriptor!);

    public WistBackendDescriptor GetRequiredDescriptor(BackendId backend)
    {
        if (_byId.TryGetValue(backend, out var descriptor))
            return descriptor;

        throw new InvalidOperationException(
            $"Wist has no descriptor for backend '{backend.Value}'. Describe it in WistBackends along with " +
            $"its intrinsics, pipeline component and artifact projection. Described backends: " +
            $"{string.Join(", ", All.Select(static x => x.Id.Value))}.");
    }
}
