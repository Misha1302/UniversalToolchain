namespace UniversalToolchain.Features.Abstractions;

/// <summary>
///     Describes the kind of user-facing symbol contributed by a feature.
/// </summary>
public enum LanguageFeatureSymbolKind
{
    SyntaxForm,
    Function,
    Type,
    RuleForm,
    Operator,
    HostBinding
}
