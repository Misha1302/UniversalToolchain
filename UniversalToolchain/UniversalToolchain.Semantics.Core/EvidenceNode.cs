namespace UniversalToolchain.Semantics.Core;

/// <summary>
/// One node of the machine-checkable reason for a judgment.
/// </summary>
/// <remarks>
/// Children are ordered by <see cref="SemanticProposition.CompareTo"/>, so the same knowledge base
/// always produces byte-identical evidence even when providers publish in a different order. The DAG is
/// a record, not an authority: a consumer may display it, but must still branch on
/// <see cref="SemanticAnswer.Judgment"/>.
/// </remarks>
public sealed class EvidenceNode
{
    private static readonly IReadOnlyList<EvidenceNode> NoChildren = Array.Empty<EvidenceNode>();

    public EvidenceNode(
        SemanticProposition conclusion,
        EvidenceOrigin origin,
        IEnumerable<EvidenceNode>? premises = null,
        SemanticProviderId? provider = null,
        SemanticTrust trust = SemanticTrust.Unknown,
        SemanticScopeId? scope = null,
        CompilationEpoch? epoch = null,
        int depth = 0)
    {
        ArgumentNullException.ThrowIfNull(conclusion);

        var materialized = (premises ?? NoChildren).OrderBy(static node => node.Conclusion).ToArray();

        Conclusion = conclusion;
        Origin = origin;
        Premises = Array.AsReadOnly(materialized);
        Provider = provider;
        Trust = trust;
        Scope = scope;
        Epoch = epoch;
        Depth = depth;
    }

    public SemanticProposition Conclusion { get; }

    public EvidenceOrigin Origin { get; }

    /// <summary>Premises in canonical order.</summary>
    public IReadOnlyList<EvidenceNode> Premises { get; }

    /// <summary>The provider that supplied the asserting fact, if this is a leaf.</summary>
    public SemanticProviderId? Provider { get; }

    public SemanticTrust Trust { get; }

    public SemanticScopeId? Scope { get; }

    public CompilationEpoch? Epoch { get; }

    /// <summary>Number of rule applications between this node and a leaf.</summary>
    public int Depth { get; }

    /// <summary>Stable, human-diffable rendering used by golden tests.</summary>
    public string Render()
    {
        var builder = new System.Text.StringBuilder();
        Append(builder, this, indent: 0);
        return builder.ToString();
    }

    private static void Append(System.Text.StringBuilder builder, EvidenceNode node, int indent)
    {
        builder.Append(' ', indent * 2);
        builder.Append(node.Conclusion);
        builder.Append(" [");
        builder.Append(node.Origin);
        if (node.Provider is { } provider)
        {
            builder.Append("; ");
            builder.Append(provider);
        }

        builder.Append(']');
        builder.Append('\n');
        foreach (var premise in node.Premises)
            Append(builder, premise, indent + 1);
    }
}
