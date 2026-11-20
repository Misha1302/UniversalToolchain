using UniversalToolchain.Semantics.Core;

namespace UniversalToolchain.Semantics.Core.Tests;

/// <summary>
/// Vocabulary shared by the spike tests. Predicates are declared once so tests cannot drift from the
/// catalog the engine actually sees.
/// </summary>
internal static class SpikeVocabulary
{
    public static readonly SemanticScopeId Scope = new("module:test");
    public static readonly SemanticScopeId OtherScope = new("module:other");

    public static readonly SemanticPredicateId ExactTarget = new("ExactTarget", 2);
    public static readonly SemanticPredicateId SameValue = new("SameValue", 2);
    public static readonly SemanticPredicateId Range = new("Range", 3);
    public static readonly SemanticPredicateId CapturesNothing = new("CapturesNothing", 1);
    public static readonly SemanticPredicateId Chain0 = new("Chain0", 1);
    public static readonly SemanticPredicateId Chain1 = new("Chain1", 1);
    public static readonly SemanticPredicateId Chain2 = new("Chain2", 1);
    public static readonly SemanticPredicateId Chain3 = new("Chain3", 1);

    public static SemanticCatalog Catalog()
    {
        var catalog = new SemanticCatalog();
        catalog.Add(new SemanticPredicateDeclaration(ExactTarget, SemanticPredicateKind.Relation));
        catalog.Add(new SemanticPredicateDeclaration(SameValue, SemanticPredicateKind.Relation));
        // Ranges are functional per variable: one variable has one range, so two different ranges for
        // the same variable are a contradiction rather than two unrelated facts.
        catalog.Add(new SemanticPredicateDeclaration(Range, SemanticPredicateKind.Functional, keyArity: 1));
        catalog.Add(new SemanticPredicateDeclaration(CapturesNothing, SemanticPredicateKind.Relation));
        catalog.Add(new SemanticPredicateDeclaration(Chain0, SemanticPredicateKind.Relation));
        catalog.Add(new SemanticPredicateDeclaration(Chain1, SemanticPredicateKind.Relation));
        catalog.Add(new SemanticPredicateDeclaration(Chain2, SemanticPredicateKind.Relation));
        catalog.Add(new SemanticPredicateDeclaration(Chain3, SemanticPredicateKind.Relation));
        return catalog;
    }

    public static SemanticProposition Proposition(SemanticPredicateId predicate, params string[] arguments) =>
        new(predicate, arguments.Select(static argument => new SemanticEntityId(argument)));

    public static SemanticProposition Proposition(SemanticPredicateId predicate, params SemanticEntityId[] arguments) =>
        new(predicate, arguments);
}
