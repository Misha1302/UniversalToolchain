namespace UniversalToolchain.Dialects.Core;

internal sealed class DialectSyntaxNormalizationResult
{
    public required IReadOnlyList<string> ActiveModules { get; init; }

    public required IReadOnlyDictionary<DialectBackendId, bool> BackendMap { get; init; }

    public required IReadOnlyList<IntrinsicBuildDirective> IntrinsicDirectives { get; init; }

    public required IReadOnlyList<OptimizerBuildDirective> OptimizerDirectives { get; init; }

    public required IReadOnlyList<DialectOrderConstraint> OrderConstraints { get; init; }
}