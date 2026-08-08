namespace UniversalToolchain.Language.Abstractions;

public readonly record struct LanguageIntrinsicId
{
    public LanguageIntrinsicId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public enum LanguageContributionOrderKind
{
    Requires,
    Before,
    After
}

public sealed record LanguageContributionOrderConstraint
{
    public LanguageContributionOrderConstraint(
        LanguageContributionOrderKind kind,
        LanguageContributionId source,
        LanguageContributionId target)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (source == target)
            throw new ArgumentException("A contribution cannot order itself relative to itself.", nameof(target));

        Kind = kind;
        Source = source;
        Target = target;
    }

    public LanguageContributionOrderKind Kind { get; }
    public LanguageContributionId Source { get; }
    public LanguageContributionId Target { get; }
}

public sealed record LanguageIntrinsicPolicyDirective
{
    public LanguageIntrinsicPolicyDirective(
        LanguageIntrinsicId intrinsic,
        bool allowed,
        BackendId? backend = null)
    {
        Intrinsic = intrinsic;
        Allowed = allowed;
        Backend = backend;
    }

    public LanguageIntrinsicId Intrinsic { get; }
    public bool Allowed { get; }
    public BackendId? Backend { get; }
}
