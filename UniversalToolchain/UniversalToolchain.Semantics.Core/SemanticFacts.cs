namespace UniversalToolchain.Semantics.Core;

/// <summary>
/// The four answers a consumer must be able to handle.
/// </summary>
/// <remarks>
/// <see cref="Proved"/> and <see cref="Disproved"/> are deliberately not complements: "not proved" is
/// <see cref="Unknown"/>, and two providers disagreeing is <see cref="Inconsistent"/> — a defect signal
/// that must never be silently folded into <see cref="Unknown"/>.
/// </remarks>
public enum Judgment
{
    Proved,
    Disproved,
    Unknown,
    Inconsistent
}

/// <summary>How an assertion came to exist. Provenance only; it grants no authority.</summary>
public enum EvidenceOrigin
{
    /// <summary>A provider asserted it as its own primitive axiom.</summary>
    DeclaredByOwner,

    /// <summary>A verifier established it by inspecting the program.</summary>
    EstablishedByVerifier,

    /// <summary>The engine derived it by applying a rule.</summary>
    DerivedByRule
}

/// <summary>
/// Everything a provider needs to publish facts that belong to the right scope and epoch.
/// </summary>
public sealed record SemanticFactContext(
    SemanticScopeId Scope,
    CompilationEpoch Epoch,
    SemanticProviderId Provider);

/// <summary>
/// One primitive assertion published by an owning provider.
/// </summary>
/// <remarks>
/// <see cref="Epoch"/> is stamped at publication, not inferred later. A fact from an earlier epoch
/// cannot answer a query in a later epoch, which is how mutation invalidation works without any manual
/// bookkeeping. <see cref="IsNegative"/> is the only sanctioned route to <see cref="Judgment.Disproved"/>
/// in v1: absence of a fact is never a disproof.
/// </remarks>
public sealed record SemanticFact
{
    public SemanticFact(
        SemanticProposition proposition,
        SemanticFactContext context,
        bool isNegative = false,
        EvidenceOrigin origin = EvidenceOrigin.DeclaredByOwner,
        SemanticTrust trust = SemanticTrust.Verified)
    {
        ArgumentNullException.ThrowIfNull(proposition);
        ArgumentNullException.ThrowIfNull(context);
        if (origin == EvidenceOrigin.DerivedByRule)
            throw new ArgumentException("A published fact cannot claim a derived origin.", nameof(origin));
        if (!Enum.IsDefined(trust))
            throw new ArgumentOutOfRangeException(nameof(trust));

        Proposition = proposition;
        Context = context;
        IsNegative = isNegative;
        Origin = origin;
        Trust = trust;
    }

    public SemanticProposition Proposition { get; }

    public SemanticFactContext Context { get; }

    public bool IsNegative { get; }

    public EvidenceOrigin Origin { get; }

    public SemanticTrust Trust { get; }

    public SemanticScopeId Scope => Context.Scope;

    public CompilationEpoch Epoch => Context.Epoch;

    public SemanticProviderId Provider => Context.Provider;

    /// <summary>Creates the same assertion in a later epoch. Used when a provider re-establishes a fact.</summary>
    public SemanticFact Reestablish(CompilationEpoch epoch) =>
        new(Proposition, Context with { Epoch = epoch }, IsNegative, Origin, Trust);
}

/// <summary>
/// Implemented by a module that knows something about its own constructs. A provider may only publish
/// facts it owns; it never selects contributions.
/// </summary>
public interface ISemanticFactProvider
{
    SemanticProviderId Id { get; }

    IReadOnlyCollection<SemanticFact> DeclarePrimitiveFacts(SemanticFactContext context);
}
