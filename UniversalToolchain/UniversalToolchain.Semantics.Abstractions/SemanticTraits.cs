namespace UniversalToolchain.Semantics.Abstractions;

[Flags]
public enum AlgebraicTraits
{
    None = 0,
    Commutative = 1 << 0,
    Associative = 1 << 1,
    Idempotent = 1 << 2,
    HasIdentityElement = 1 << 3,
    HasAbsorbingElement = 1 << 4,
    Comparison = 1 << 5,
    Monotonic = 1 << 6,
    Invertible = 1 << 7
}

[Flags]
public enum SemanticTypeTraits
{
    None = 0,
    Predicate = 1 << 0,
    Numeric = 1 << 1,
    Reference = 1 << 2,
    ValueObject = 1 << 3,
    Nullable = 1 << 4,
    HasIdentity = 1 << 5,
    Immutable = 1 << 6,
    RuntimeManaged = 1 << 7,
    BackendNative = 1 << 8
}

public enum Determinism
{
    Deterministic,
    NonDeterministic,
    Unknown
}

public enum SemanticTrustLevel
{
    BuiltInTrusted,
    VerifiedPlugin,
    UserProvidedUnchecked,
    ExternalUnknown
}
