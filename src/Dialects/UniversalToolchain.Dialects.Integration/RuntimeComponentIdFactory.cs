namespace UniversalToolchain.Dialects.Integration;

public static class RuntimeComponentIdFactory
{
    public static RuntimeComponentId Create(RuntimeComponentKind kind, string canonicalAlias)
    {
        var prefix = kind switch
        {
            RuntimeComponentKind.FrontendModule => "frontend",
            RuntimeComponentKind.Optimizer => "optimizer",
            RuntimeComponentKind.Backend => "backend",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported runtime component kind.")
        };

        return new RuntimeComponentId($"{prefix}.{canonicalAlias.Trim().ToLowerInvariant()}");
    }
}