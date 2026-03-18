namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveDescriptor
{
    public required DialectDirectiveKind Kind { get; init; }

    public required string Keyword { get; init; }

    public required DialectDirectiveArgumentShape ArgumentShape { get; init; }

    public bool IsSingleton { get; init; }
}

public static class DialectDirectiveDescriptors
{
    private static readonly Lazy<IReadOnlyList<DialectDirectiveDescriptor>> _ordered = new(() =>
        DialectDslFeatureCatalog.Features
            .Select(x => new DialectDirectiveDescriptor
            {
                Kind = x.Kind,
                Keyword = x.Keyword,
                ArgumentShape = x.ArgumentShape,
                IsSingleton = x.IsSingleton
            })
            .ToList());

    private static readonly Lazy<IReadOnlyDictionary<DialectDirectiveKind, DialectDirectiveDescriptor>> _byKind = new(() =>
        _ordered.Value.ToDictionary(x => x.Kind));

    private static readonly Lazy<IReadOnlyDictionary<string, DialectDirectiveDescriptor>> _byKeyword = new(() =>
        _ordered.Value.ToDictionary(x => x.Keyword, StringComparer.Ordinal));

    public static IReadOnlyList<DialectDirectiveDescriptor> Ordered => _ordered.Value;

    public static DialectDirectiveDescriptor Get(DialectDirectiveKind kind)
    {
        return _byKind.Value[kind];
    }

    public static bool TryGetByKeyword(string keyword, out DialectDirectiveDescriptor descriptor)
    {
        return _byKeyword.Value.TryGetValue(keyword, out descriptor!);
    }
}
