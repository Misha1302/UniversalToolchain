using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

/// <summary>
/// Declares semantic properties for a managed method or constructor that is exposed
/// to SSA as a regular callable.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
public sealed class SsaManagedCallableAttribute : Attribute
{
    /// <summary>
    /// Marks the callable as having no observable effects.
    /// </summary>
    public bool IsPure { get; set; }

    public bool ReadsRuntimeState { get; set; }

    public bool WritesRuntimeState { get; set; }

    public bool ReadsMemory { get; set; }

    public bool WritesMemory { get; set; }

    public bool Allocates { get; set; }

    public bool MayThrow { get; set; } = true;

    public bool CallsExternalCode { get; set; } = true;

    public bool UnknownExternalEffect { get; set; } = true;

    public Determinism Determinism { get; set; } = Determinism.Unknown;

    public AlgebraicTraits AlgebraicTraits { get; set; } = AlgebraicTraits.None;

    public SemanticTrustLevel TrustLevel { get; set; } = SemanticTrustLevel.ExternalUnknown;
}
