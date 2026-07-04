using System.Collections.ObjectModel;

namespace UniversalToolchain.Ssa.Abstractions;

public sealed record SsaAttribute(SsaAttributeKey Key, string Value);

public sealed class SsaAttributeBag
{
    private readonly ReadOnlyCollection<SsaAttribute> _attributes;
    private readonly Dictionary<SsaAttributeKey, SsaAttribute> _byKey;

    public SsaAttributeBag(IEnumerable<SsaAttribute>? attributes = null)
    {
        var ordered = (attributes ?? [])
            .OrderBy(static x => x.Key)
            .ToList();

        _byKey = new Dictionary<SsaAttributeKey, SsaAttribute>();
        foreach (var attribute in ordered)
        {
            if (!_byKey.TryAdd(attribute.Key, attribute))
                throw new ArgumentException($"Duplicate SSA attribute key '{attribute.Key}'.", nameof(attributes));
        }

        _attributes = new ReadOnlyCollection<SsaAttribute>(ordered);
    }

    public static SsaAttributeBag Empty { get; } = new();

    public IReadOnlyList<SsaAttribute> Values => _attributes;

    public bool Contains(SsaAttributeKey key) => _byKey.ContainsKey(key);

    public bool TryGet(SsaAttributeKey key, out SsaAttribute attribute) => _byKey.TryGetValue(key, out attribute!);
}
