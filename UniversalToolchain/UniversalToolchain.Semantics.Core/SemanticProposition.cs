using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Semantics.Core;

/// <summary>
/// Identity of one semantic predicate: a name plus a fixed arity.
/// </summary>
/// <remarks>
/// Arity is part of the identity because a query for a one-argument wrong shape must not silently
/// resolve to a two-argument fact. Mismatched arity is a caller defect, reported as
/// <c>UTL2405</c>.
/// </remarks>
public readonly record struct SemanticPredicateId : IComparable<SemanticPredicateId>
{
    public SemanticPredicateId(string name, int arity)
    {
        Name = SemanticValueValidation.Required(name, nameof(name));
        if (arity < 0)
            throw new ArgumentOutOfRangeException(nameof(arity), "Predicate arity must not be negative.");

        Arity = arity;
    }

    public string Name { get; }

    public int Arity { get; }

    public int CompareTo(SemanticPredicateId other)
    {
        var byName = string.CompareOrdinal(Name, other.Name);
        return byName != 0 ? byName : Arity.CompareTo(other.Arity);
    }

    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Name}/{Arity}");
}

/// <summary>
/// A ground assertion about the program, for example <c>ExactTarget(call#12, lambda#3)</c>.
/// </summary>
/// <remarks>
/// There are no variables in v1. Rules only chain ground propositions, which is why the engine needs
/// no unification and stays monotone by construction.
/// </remarks>
public sealed class SemanticProposition : IEquatable<SemanticProposition>, IComparable<SemanticProposition>
{
    public SemanticProposition(SemanticPredicateId predicate, params SemanticEntityId[] arguments)
        : this(predicate, (IEnumerable<SemanticEntityId>)arguments)
    {
    }

    public SemanticProposition(SemanticPredicateId predicate, IEnumerable<SemanticEntityId> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var materialized = arguments.ToArray();
        if (materialized.Length != predicate.Arity)
            throw new ArgumentException(
                $"Predicate '{predicate}' declares arity {predicate.Arity} but received {materialized.Length} arguments.",
                nameof(arguments));

        Predicate = predicate;
        Arguments = Array.AsReadOnly(materialized);
    }

    public SemanticPredicateId Predicate { get; }

    public IReadOnlyList<SemanticEntityId> Arguments { get; }

    public bool Equals(SemanticProposition? other)
    {
        if (other is null || !Predicate.Equals(other.Predicate) || Arguments.Count != other.Arguments.Count)
            return false;

        for (var index = 0; index < Arguments.Count; index++)
        {
            if (!Arguments[index].Equals(other.Arguments[index]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as SemanticProposition);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Predicate);
        foreach (var argument in Arguments)
            hash.Add(argument);

        return hash.ToHashCode();
    }

    public int CompareTo(SemanticProposition? other)
    {
        if (other is null)
            return 1;

        var byPredicate = Predicate.CompareTo(other.Predicate);
        if (byPredicate != 0)
            return byPredicate;

        for (var index = 0; index < Arguments.Count; index++)
        {
            var byArgument = Arguments[index].CompareTo(other.Arguments[index]);
            if (byArgument != 0)
                return byArgument;
        }

        return 0;
    }

    public override string ToString() => $"{Predicate}({string.Join(", ", Arguments)})";
}

public enum SemanticPredicateKind
{
    /// <summary>Any number of argument tuples may hold.</summary>
    Relation,

    /// <summary>At most one tuple may hold for one key. A second, different tuple is a contradiction.</summary>
    Functional
}

/// <summary>
/// Declares how a predicate may be interpreted by the engine.
/// </summary>
/// <remarks>
/// Functionality is the only way v1 can detect a contradiction between two *positive* axioms, which is
/// scenario 3 of <c>UT-A026</c>: two packages publish <c>Range(v,3,9)</c> and <c>Range(v,100,200)</c>.
/// Nothing here grants selection authority; a declaration is pure metadata.
/// </remarks>
public sealed class SemanticPredicateDeclaration
{
    public SemanticPredicateDeclaration(SemanticPredicateId id, SemanticPredicateKind kind, int keyArity = 0)
    {
        if (kind == SemanticPredicateKind.Functional)
        {
            if (keyArity < 1 || keyArity >= id.Arity)
                throw new ArgumentOutOfRangeException(
                    nameof(keyArity),
                    "A functional predicate needs at least one key argument and at least one value argument.");
        }
        else if (keyArity != 0)
        {
            throw new ArgumentException("Only functional predicates declare a key arity.", nameof(keyArity));
        }

        Id = id;
        Kind = kind;
        KeyArity = keyArity;
    }

    public SemanticPredicateId Id { get; }

    public SemanticPredicateKind Kind { get; }

    public int KeyArity { get; }

    public SemanticEntityId KeyOf(SemanticProposition proposition) =>
        new(string.Join('\u001f', proposition.Arguments.Take(KeyArity).Select(static argument => argument.Value)));

    public SemanticEntityId ValueOf(SemanticProposition proposition) =>
        new(string.Join('\u001f', proposition.Arguments.Skip(KeyArity).Select(static argument => argument.Value)));
}

/// <summary>
/// The predicate universe known to one knowledge base. Merge is order-independent and refuses
/// two declarations that disagree about the same predicate.
/// </summary>
public sealed class SemanticCatalog
{
    private readonly SortedDictionary<SemanticPredicateId, SemanticPredicateDeclaration> _declarations = new();

    public SemanticCatalog Add(SemanticPredicateDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        if (_declarations.TryGetValue(declaration.Id, out var existing))
        {
            if (existing.Kind != declaration.Kind || existing.KeyArity != declaration.KeyArity)
            {
                throw new InvalidOperationException(
                    $"Predicate '{declaration.Id}' is declared twice with different semantics.");
            }

            return this;
        }

        _declarations.Add(declaration.Id, declaration);
        return this;
    }

    public SemanticPredicateDeclaration? Find(SemanticPredicateId id) =>
        _declarations.TryGetValue(id, out var declaration) ? declaration : null;

    public IReadOnlyCollection<SemanticPredicateDeclaration> Declarations => _declarations.Values;

    internal static SemanticCatalog Empty() => new();
}

/// <summary>
/// Diagnostic codes owned by the semantic core. They extend the <c>UTL2x</c> family documented in
/// <c>docs/reference/diagnostics.md</c>; <c>UTL2401</c>-<c>UTL2403</c> are reserved for obligation
/// outcomes so that a later planner-side admissibility check can reuse them without renaming.
/// </summary>
public static class SemanticDiagnosticCodes
{
    public const string ObligationUnknown = "UTL2401";
    public const string ObligationDisproved = "UTL2402";
    public const string ObligationInconsistent = "UTL2403";
    public const string DerivationDepthExceeded = "UTL2404";
    public const string PredicateArityMismatch = "UTL2405";
    public const string EvidenceStale = "UTL2406";
    public const string TrustPrecedence = "UTL2407";
}
