namespace UniversalToolchain.Wist;

/// <summary>Controls source text retained in durable Wist program metadata.</summary>
public enum WistSourceRetentionPolicy
{
    Full,
    HashAndIdentity,
    None
}

/// <summary>Controls whether expected-failure results expose developer exception objects and raw messages.</summary>
public enum WistDiagnosticExposure
{
    Safe,
    Developer
}
