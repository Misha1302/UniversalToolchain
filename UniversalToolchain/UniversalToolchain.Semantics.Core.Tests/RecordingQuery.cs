using UniversalToolchain.Semantics.Core;

namespace UniversalToolchain.Semantics.Core.Tests;

/// <summary>
/// Records what the engine asked a query for, so tests can prove that a decision came from the query
/// surface rather than from a hardcoded path.
/// </summary>
internal sealed class RecordingQuery(ISemanticQuery inner) : ISemanticQuery
{
    public List<SemanticProposition> Asked { get; } = [];

    public CompilationEpoch Epoch => inner.Epoch;

    public SemanticAnswer Ask(SemanticProposition question)
    {
        Asked.Add(question);
        return inner.Ask(question);
    }

    public IReadOnlyList<SemanticAnswer> AskAll(IEnumerable<SemanticProposition> questions)
    {
        var materialized = questions.ToArray();
        Asked.AddRange(materialized);
        return inner.AskAll(materialized);
    }
}
