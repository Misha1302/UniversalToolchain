using System.Collections.ObjectModel;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ExpressionTyping.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class ExpressionTypeRuleCatalog
{
    private readonly ReadOnlyCollection<ToolchainDiagnostic> _diagnostics;
    private readonly ReadOnlyCollection<IExpressionTypeRule> _rules;

    public ExpressionTypeRuleCatalog(IEnumerable<IExpressionTypeRule> rules, IEnumerable<ToolchainDiagnostic>? diagnostics = null)
    {
        if (rules is null)
            throw new ArgumentNullException(nameof(rules));

        _rules = new ReadOnlyCollection<IExpressionTypeRule>(rules
            .OrderBy(static x => CapabilityProviderTypeResolver.GetTypeName(x.GetType()), StringComparer.Ordinal)
            .ToList());
        _diagnostics = new ReadOnlyCollection<ToolchainDiagnostic>((diagnostics ?? [])
            .OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Message, StringComparer.Ordinal)
            .ToList());
    }

    public IReadOnlyList<IExpressionTypeRule> Rules => _rules;

    public IReadOnlyList<ToolchainDiagnostic> Diagnostics => _diagnostics;
}