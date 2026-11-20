namespace UniversalToolchain.Semantics.Core;

/// <summary>
/// How much a consumer may trust a published assertion.
/// </summary>
/// <remarks>
/// This deliberately does not reuse <c>UniversalToolchain.Semantics.Abstractions.SemanticTrustLevel</c>.
/// ADR-003 keeps the core dependent on <c>Language.Abstractions</c> only, so that the descriptor
/// assembly can keep evolving for SSA consumers without dragging an inference engine along. Consumers
/// that already speak the descriptor vocabulary map their level at the boundary; the ordering below is
/// the contract those mappings must preserve.
/// </remarks>
public enum SemanticTrust
{
    /// <summary>Declared by a shipped, first-party provider.</summary>
    BuiltIn = 3,

    /// <summary>Declared by a provider that passed verification.</summary>
    Verified = 2,

    /// <summary>Declared by user or third-party code that was not checked.</summary>
    Unchecked = 1,

    /// <summary>Trust is not established. Treated as the weakest level.</summary>
    Unknown = 0
}
