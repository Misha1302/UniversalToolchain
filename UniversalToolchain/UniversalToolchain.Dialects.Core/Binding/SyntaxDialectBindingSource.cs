using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core.Binding;

internal sealed class SyntaxDialectBindingSource : IDialectBindingSource
{
    private readonly ReadOnlyCollection<BackendBindingDirectiveRecord> _backendDirectives;
    private readonly ReadOnlyCollection<KeyValuePair<string, bool>> _capabilities;
    private readonly DialectSyntaxDocument _document;
    private readonly ReadOnlyCollection<IntrinsicBindingDirectiveRecord> _intrinsicDirectives;
    private readonly ReadOnlyCollection<OptimizerBindingDirectiveRecord> _optimizerDirectives;
    private readonly ReadOnlyCollection<OrderBindingDirectiveRecord> _orderRules;

    public SyntaxDialectBindingSource(DialectSyntaxDocument document)
    {
        document = document.ArgNotNull();

        _document = document;
        _orderRules = new ReadOnlyCollection<OrderBindingDirectiveRecord>(document.OrderRules.Select(ToOrderRule).ToList());
        _backendDirectives = new ReadOnlyCollection<BackendBindingDirectiveRecord>(document.BackendDirectives.Select(ToBackendDirective).ToList());
        _intrinsicDirectives = new ReadOnlyCollection<IntrinsicBindingDirectiveRecord>(document.IntrinsicDirectives.Select(ToIntrinsicDirective).ToList());
        _optimizerDirectives = new ReadOnlyCollection<OptimizerBindingDirectiveRecord>(document.OptimizerDirectives.Select(ToOptimizerDirective).ToList());
        _capabilities = new ReadOnlyCollection<KeyValuePair<string, bool>>(document.Capabilities.ToList());
    }

    public DialectBindingInputKind InputKind => DialectBindingInputKind.Syntax;

    public string Name => _document.Name;

    public string? Version => _document.Version;

    public string? BaseDialectName => _document.BaseDialectName;

    public IReadOnlyList<string> UseModules => _document.UseModules;

    public IReadOnlyList<string> ExcludeModules => _document.ExcludeModules;

    public IReadOnlyList<OrderBindingDirectiveRecord> OrderRules => _orderRules;

    public IReadOnlyList<BackendBindingDirectiveRecord> BackendDirectives => _backendDirectives;

    public IReadOnlyList<IntrinsicBindingDirectiveRecord> IntrinsicDirectives => _intrinsicDirectives;

    public IReadOnlyList<OptimizerBindingDirectiveRecord> OptimizerDirectives => _optimizerDirectives;

    public SecurityProfile? SecurityProfile => _document.SecurityProfile;

    public IReadOnlyList<KeyValuePair<string, bool>> Capabilities => _capabilities;

    private static OrderBindingDirectiveRecord ToOrderRule(OrderRule rule) => new(rule.Kind, rule.ModuleName, rule.RelatedModuleName);

    private static BackendBindingDirectiveRecord ToBackendDirective(BackendDirectiveSyntax directive) => new(directive.Backend, directive.Enabled);

    private static IntrinsicBindingDirectiveRecord ToIntrinsicDirective(IntrinsicDirectiveSyntax directive) => new(directive.Name, directive.Target, directive.Allowed);

    private static OptimizerBindingDirectiveRecord ToOptimizerDirective(OptimizerDirectiveSyntax directive) => new(directive.Name, directive.Target, directive.Enabled);
}