namespace UniversalToolchain.Semantics.Core;

/// <summary>
/// A declarative, monotone inference rule: if every body proposition is proved, the head is proved.
/// </summary>
/// <remarks>
/// v1 rules are ground — there are no variables and no unification — and every head is positive.
/// Negation-as-failure is deliberately absent, so absence of a body can never derive anything. This is
/// what keeps "not proved here" from silently becoming "disproved".
/// </remarks>
public sealed class SemanticRule
{
    public SemanticRule(
        SemanticProviderId provider,
        SemanticProposition head,
        IEnumerable<SemanticProposition> body,
        SemanticTrust minimumTrust = SemanticTrust.Unchecked,
        bool isNegativeHead = false)
    {
        ArgumentNullException.ThrowIfNull(head);
        ArgumentNullException.ThrowIfNull(body);
        if (!Enum.IsDefined(minimumTrust))
            throw new ArgumentOutOfRangeException(nameof(minimumTrust));

        var materialized = body.ToArray();
        if (materialized.Length == 0)
        {
            throw new ArgumentException(
                "A rule with an empty body is an axiom; publish it as a fact from its owner instead.",
                nameof(body));
        }

        Provider = provider;
        Head = head;
        Body = Array.AsReadOnly(materialized);
        MinimumTrust = minimumTrust;
        IsNegativeHead = isNegativeHead;
    }

    public SemanticProviderId Provider { get; }

    public SemanticProposition Head { get; }

    public IReadOnlyList<SemanticProposition> Body { get; }

    /// <summary>
    /// When true, a satisfied body derives the *negative* judgment for <see cref="Head"/>. Negative heads
    /// are the second sanctioned route to <see cref="Judgment.Disproved"/>; they still require positive
    /// body evidence, so this is not negation-as-failure.
    /// </summary>
    public bool IsNegativeHead { get; }

    /// <summary>The weakest body evidence this rule is willing to accept.</summary>
    public SemanticTrust MinimumTrust { get; }
}

/// <summary>
/// Implemented by a module that wants to publish inference rules for its own vocabulary.
/// Rules are pure data; they cannot observe or mutate anything.
/// </summary>
public interface ISemanticRuleProvider
{
    SemanticProviderId Id { get; }

    IReadOnlyCollection<SemanticRule> Rules { get; }
}
