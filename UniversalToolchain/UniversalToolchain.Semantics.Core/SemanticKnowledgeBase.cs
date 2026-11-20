using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Semantics.Core;

/// <summary>
/// Accumulates facts and rules, then answers queries against one epoch snapshot.
/// </summary>
/// <remarks>
/// <para>Properties this spike is required to demonstrate:</para>
/// <list type="number">
/// <item>Monotone: adding facts and rules can move <see cref="Judgment.Unknown"/> to
/// <see cref="Judgment.Proved"/> or <see cref="Judgment.Inconsistent"/>, never
/// <see cref="Judgment.Proved"/> to <see cref="Judgment.Unknown"/>.</item>
/// <item>Deterministic: the evidence DAG is identical for the same knowledge base regardless of the
/// order in which providers were registered.</item>
/// <item>Bounded: a derivation deeper than <see cref="MaxDerivationDepth"/> yields
/// <see cref="Judgment.Unknown"/> with a diagnostic, never an exception or an unbounded search.</item>
/// <item>Non-selecting: nothing here can add, remove, or reorder a contribution. It only answers
/// questions.</item>
/// </list>
/// </remarks>
public sealed class SemanticKnowledgeBase : ISemanticQuery
{
    private readonly List<SemanticFact> _facts = [];
    private readonly List<SemanticRule> _rules = [];
    private readonly SemanticCatalog _catalog;
    private readonly Dictionary<SemanticProposition, SemanticAnswer> _cache = [];
    private CompilationEpoch _epoch;
    private Closure? _closure;

    public SemanticKnowledgeBase(
        SemanticCatalog? catalog = null,
        CompilationEpoch? epoch = null,
        int maxDerivationDepth = 8)
    {
        if (maxDerivationDepth < 0)
            throw new ArgumentOutOfRangeException(nameof(maxDerivationDepth), "Maximum derivation depth must not be negative.");

        _catalog = catalog ?? SemanticCatalog.Empty();
        _epoch = epoch ?? new CompilationEpoch(0);
        MaxDerivationDepth = maxDerivationDepth;
    }

    public int MaxDerivationDepth { get; }

    public CompilationEpoch Epoch => _epoch;

    /// <summary>Registers a provider's primitive facts for the current epoch and the given scope.</summary>
    public SemanticKnowledgeBase AddProvider(ISemanticFactProvider provider, SemanticScopeId scope)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var context = new SemanticFactContext(scope, _epoch, provider.Id);
        foreach (var fact in provider.DeclarePrimitiveFacts(context))
        {
            if (!fact.Provider.Equals(provider.Id))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider.Id}' published a fact claiming to be owned by '{fact.Provider}'.");
            }

            if (!fact.Epoch.Equals(_epoch))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider.Id}' published a fact for epoch {fact.Epoch} while the current epoch is {_epoch}.");
            }

            _facts.Add(fact);
        }

        Invalidate();
        return this;
    }

    /// <summary>Registers a provider's declarative rules.</summary>
    public SemanticKnowledgeBase AddRuleProvider(ISemanticRuleProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        foreach (var rule in provider.Rules)
        {
            if (!rule.Provider.Equals(provider.Id))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider.Id}' published a rule attributed to '{rule.Provider}'.");
            }

            _rules.Add(rule);
        }

        Invalidate();
        return this;
    }

    public SemanticKnowledgeBase AddFact(SemanticFact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        if (!fact.Epoch.Equals(_epoch))
        {
            throw new InvalidOperationException(
                $"Fact for epoch {fact.Epoch} cannot be added to epoch {_epoch}.");
        }

        _facts.Add(fact);
        Invalidate();
        return this;
    }

    public SemanticKnowledgeBase AddRule(SemanticRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        Invalidate();
        return this;
    }

    /// <summary>
    /// Starts a new epoch. Every fact published in an earlier epoch becomes stale: the same query now
    /// yields <see cref="Judgment.Unknown"/> with a staleness diagnostic until a provider re-establishes
    /// the fact. Memoized answers are dropped, never carried across the boundary.
    /// </summary>
    public SemanticKnowledgeBase AdvanceEpoch()
    {
        _epoch = _epoch.Next();
        Invalidate();
        return this;
    }

    public SemanticAnswer Ask(SemanticProposition question)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (_cache.TryGetValue(question, out var cached))
            return cached;

        var answer = Solve(question);
        _cache[question] = answer;
        return answer;
    }

    public IReadOnlyList<SemanticAnswer> AskAll(IEnumerable<SemanticProposition> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);
        return questions.Select(Ask).ToArray();
    }

    private void Invalidate()
    {
        _cache.Clear();
        _closure = null;
    }

    private SemanticAnswer Solve(SemanticProposition question)
    {
        var diagnostics = new List<LanguageDiagnostic>();

        // Arity is part of predicate identity. A same-name predicate with a different arity is a caller
        // defect, not silent absence, so it is reported instead of disappearing into Unknown.
        foreach (var declaration in _catalog.Declarations)
        {
            if (string.Equals(declaration.Id.Name, question.Predicate.Name, StringComparison.Ordinal) &&
                declaration.Id.Arity != question.Predicate.Arity)
            {
                diagnostics.Add(Diagnostic(
                    SemanticDiagnosticCodes.PredicateArityMismatch,
                    LanguageDiagnosticSeverity.Error,
                    $"Predicate '{question.Predicate.Name}' is declared with arity {declaration.Id.Arity}, but the query used arity {question.Predicate.Arity}."));
            }
        }

        var allForPredicate = _facts
            .Where(fact => question.Predicate.Equals(fact.Proposition.Predicate))
            .ToArray();

        var stale = allForPredicate.Count(fact => !fact.Epoch.Equals(_epoch));
        if (stale > 0)
        {
            diagnostics.Add(Diagnostic(
                SemanticDiagnosticCodes.EvidenceStale,
                LanguageDiagnosticSeverity.Warning,
                $"{stale} assertion(s) about {question.Predicate} belong to an earlier compilation epoch and were not re-established."));
        }

        var closure = GetClosure();

        if (closure.Positive.TryGetValue(question, out var proved))
        {
            // A functional predicate answered by two values for one key is a configuration defect even
            // when the queried tuple is itself established: the consumer cannot know which one holds.
            var contradiction = FindFunctionalContradiction(closure, question);
            if (contradiction is not null)
            {
                diagnostics.Add(Diagnostic(
                    SemanticDiagnosticCodes.ObligationInconsistent,
                    LanguageDiagnosticSeverity.Error,
                    contradiction));
                return new SemanticAnswer(question, Judgment.Inconsistent, null, diagnostics);
            }

            closure.Negative.TryGetValue(question, out var disproved);
            if (disproved is not null && disproved.Trust == proved.Trust)
            {
                diagnostics.Add(Diagnostic(
                    SemanticDiagnosticCodes.ObligationInconsistent,
                    LanguageDiagnosticSeverity.Error,
                    $"Both a positive and a negative assertion for {question} were established at trust level {proved.Trust}."));
                return new SemanticAnswer(question, Judgment.Inconsistent, null, diagnostics);
            }

            if (disproved is not null && disproved.Trust > proved.Trust)
            {
                diagnostics.Add(Diagnostic(
                    SemanticDiagnosticCodes.TrustPrecedence,
                    LanguageDiagnosticSeverity.Warning,
                    $"A negative assertion for {question} at trust {disproved.Trust} outranks positive evidence at {proved.Trust}."));
                return new SemanticAnswer(question, Judgment.Disproved, disproved, diagnostics);
            }

            if (disproved is not null)
            {
                diagnostics.Add(Diagnostic(
                    SemanticDiagnosticCodes.TrustPrecedence,
                    LanguageDiagnosticSeverity.Warning,
                    $"Positive evidence for {question} at trust {proved.Trust} outranks a negative assertion at {disproved.Trust}."));
            }

            return new SemanticAnswer(question, Judgment.Proved, proved, diagnostics);
        }

        if (closure.Negative.TryGetValue(question, out var negative))
        {
            diagnostics.Add(Diagnostic(
                SemanticDiagnosticCodes.ObligationDisproved,
                LanguageDiagnosticSeverity.Info,
                $"{question} is Disproved by an explicit negative assertion. Absence of evidence was not used."));
            return new SemanticAnswer(question, Judgment.Disproved, negative, diagnostics);
        }

        if (closure.DepthExceeded.Contains(question))
        {
            diagnostics.Add(Diagnostic(
                SemanticDiagnosticCodes.DerivationDepthExceeded,
                LanguageDiagnosticSeverity.Warning,
                $"Deriving {question} exceeded the maximum derivation depth of {MaxDerivationDepth}; the result is Unknown, not Disproved."));
            return new SemanticAnswer(question, Judgment.Unknown, null, diagnostics);
        }

        // Scenario 2: a consumer must be able to say *which* predicate it was missing, otherwise the
        // diagnostic is useless to the author who has to fix the wiring.
        diagnostics.Add(Diagnostic(
            SemanticDiagnosticCodes.ObligationUnknown,
            LanguageDiagnosticSeverity.Info,
            $"No provider established '{question.Predicate}', so '{question}' is Unknown. Absence of evidence is not a disproof."));

        return new SemanticAnswer(question, Judgment.Unknown, null, diagnostics);
    }

    private string? FindFunctionalContradiction(Closure closure, SemanticProposition question)
    {
        var declaration = _catalog.Find(question.Predicate);
        if (declaration is null || declaration.Kind != SemanticPredicateKind.Functional)
            return null;

        var key = declaration.KeyOf(question);
        var values = closure.Positive
            .Where(entry => entry.Key.Predicate.Equals(question.Predicate))
            .Where(entry => declaration.KeyOf(entry.Key).Equals(key))
            .Select(entry => (Value: declaration.ValueOf(entry.Key), entry.Value.Trust))
            .ToArray();

        if (values.Length < 2)
            return null;

        var highestTrust = values.Max(static value => value.Trust);
        var distinct = values
            .Where(value => value.Trust == highestTrust)
            .Select(static value => value.Value)
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();

        if (distinct.Length < 2)
            return null;

        return $"Functional predicate '{question.Predicate}' has {distinct.Length} different values for key '{key}' at trust level {highestTrust}: " +
               string.Join(", ", distinct);
    }

    private Closure GetClosure()
    {
        if (_closure is not null)
            return _closure;

        var positive = new SortedDictionary<SemanticProposition, EvidenceNode>(PropositionOrder.Instance);
        var negative = new SortedDictionary<SemanticProposition, EvidenceNode>(PropositionOrder.Instance);
        var depthExceeded = new SortedSet<SemanticProposition>(PropositionOrder.Instance);

        bool Consider(IDictionary<SemanticProposition, EvidenceNode> target, EvidenceNode candidate)
        {
            if (candidate.Depth > MaxDerivationDepth)
            {
                depthExceeded.Add(candidate.Conclusion);
                return false;
            }

            if (target.TryGetValue(candidate.Conclusion, out var existing) &&
                EvidenceOrder.Instance.Compare(candidate, existing) >= 0)
            {
                return false;
            }

            target[candidate.Conclusion] = candidate;
            return true;
        }

        foreach (var fact in _facts)
        {
            if (!fact.Epoch.Equals(_epoch))
                continue;

            var node = new EvidenceNode(
                fact.Proposition,
                fact.Origin,
                premises: null,
                provider: fact.Provider,
                trust: fact.Trust,
                scope: fact.Scope,
                epoch: fact.Epoch,
                depth: 0);

            Consider(fact.IsNegative ? negative : positive, node);
        }

        // Canonical rule order so the fixed point cannot depend on registration order.
        var rules = _rules
            .OrderBy(static rule => rule.Head, PropositionOrder.Instance)
            .ThenBy(static rule => rule.Provider)
            .ThenBy(static rule => rule.Body.Count)
            .ToArray();

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var rule in rules)
            {
                var premises = new EvidenceNode[rule.Body.Count];
                var trust = SemanticTrust.BuiltIn;

                for (var index = 0; index < rule.Body.Count; index++)
                {
                    if (!positive.TryGetValue(rule.Body[index], out var node))
                        goto NextRule;

                    premises[index] = node;
                    if (node.Trust < trust)
                        trust = node.Trust;
                }

                if (trust < rule.MinimumTrust)
                    continue;

                var depth = premises.Max(static node => node.Depth) + 1;
                var head = new EvidenceNode(
                    rule.Head,
                    EvidenceOrigin.DerivedByRule,
                    premises,
                    provider: rule.Provider,
                    trust: trust,
                    depth: depth);

                var target = rule.IsNegativeHead ? negative : positive;
                if (Consider(target, head))
                    changed = true;

            NextRule: ;
            }
        }

        _closure = new Closure(positive, negative, depthExceeded);
        return _closure;
    }

    private sealed record Closure(
        IReadOnlyDictionary<SemanticProposition, EvidenceNode> Positive,
        IReadOnlyDictionary<SemanticProposition, EvidenceNode> Negative,
        ISet<SemanticProposition> DepthExceeded);

    private static LanguageDiagnostic Diagnostic(string code, LanguageDiagnosticSeverity severity, string message) =>
        new(code, severity, "semantics", message);

    internal sealed class PropositionOrder : IComparer<SemanticProposition>
    {
        public static PropositionOrder Instance { get; } = new();

        public int Compare(SemanticProposition? x, SemanticProposition? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            return y is null ? 1 : x.CompareTo(y);
        }
    }

    /// <summary>
    /// Total order used to pick one canonical witness when several derivations exist. Shallower and
    /// more direct evidence wins, which keeps the DAG stable and human-readable.
    /// </summary>
    internal sealed class EvidenceOrder : IComparer<EvidenceNode>
    {
        public static EvidenceOrder Instance { get; } = new();

        public int Compare(EvidenceNode? x, EvidenceNode? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            var byDepth = x.Depth.CompareTo(y.Depth);
            if (byDepth != 0)
                return byDepth;

            var byOrigin = x.Origin.CompareTo(y.Origin);
            if (byOrigin != 0)
                return byOrigin;

            var byProvider = Nullable.Compare(x.Provider, y.Provider);
            if (byProvider != 0)
                return byProvider;

            return string.CompareOrdinal(x.Render(), y.Render());
        }
    }
}
