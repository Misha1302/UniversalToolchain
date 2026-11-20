using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Semantics.Core;

/// <summary>
/// What a consumer receives. The judgment is the answer; the proof is supporting detail.
/// </summary>
/// <remarks>
/// A consumer must branch on all four judgments. Treating <see cref="Judgment.Proved"/> as the only
/// interesting value and everything else as "skip" is permitted for a *refusal* path, but any consumer
/// that acts on an answer must refuse on <see cref="Judgment.Inconsistent"/> rather than degrade it to
/// <see cref="Judgment.Unknown"/>.
/// </remarks>
public sealed record SemanticAnswer(
    SemanticProposition Question,
    Judgment Judgment,
    EvidenceNode? Proof,
    IReadOnlyList<LanguageDiagnostic> Diagnostics)
{
    public bool IsProved => Judgment == Judgment.Proved;

    /// <summary>True when the consumer must refuse the transformation rather than skip it.</summary>
    public bool IsHardRefusal => Judgment == Judgment.Inconsistent;
}

/// <summary>
/// The only surface a consumer such as an optimizer sees. It exposes questions, never providers, so a
/// consumer still cannot import the package that owns a fact.
/// </summary>
public interface ISemanticQuery
{
    CompilationEpoch Epoch { get; }

    SemanticAnswer Ask(SemanticProposition question);

    /// <summary>Answers several questions against one snapshot; result order matches question order.</summary>
    IReadOnlyList<SemanticAnswer> AskAll(IEnumerable<SemanticProposition> questions);
}
