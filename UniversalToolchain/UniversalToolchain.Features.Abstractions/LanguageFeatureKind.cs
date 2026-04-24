namespace UniversalToolchain.Features.Abstractions;

/// <summary>
///     Describes the high-level category of a language feature.
/// </summary>
public enum LanguageFeatureKind
{
    Syntax,
    FunctionSet,
    TypeSystem,
    RuleModel,
    HostIntegration,
    Diagnostic,
    Optimization,
    Interop
}
