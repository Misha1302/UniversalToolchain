namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Classifies a fallback decision without embedding language-specific diagnostic codes in the generic core.
/// </summary>
public enum PlanFuzzFallbackKind
{
    None,
    ClassifiedUnsupportedShape,
    Unclassified,
    InternalFailure
}
