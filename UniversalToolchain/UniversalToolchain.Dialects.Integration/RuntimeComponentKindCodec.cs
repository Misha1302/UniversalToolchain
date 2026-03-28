using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public static class RuntimeComponentKindCodec
{
    public static RuntimeComponentKind Parse(string value, string? sourceName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(nameof(value), "Runtime component kind must not be empty.");

        var trimmed = value.Trim();
        return trimmed switch
        {
            "FrontendModule" => RuntimeComponentKind.FrontendModule,
            "Optimizer" => RuntimeComponentKind.Optimizer,
            "Backend" => RuntimeComponentKind.Backend,
            _ => ThrowUnsupported(trimmed, sourceName)
        };
    }

    public static string Format(RuntimeComponentKind kind)
    {
        return kind switch
        {
            RuntimeComponentKind.FrontendModule => "FrontendModule",
            RuntimeComponentKind.Optimizer => "Optimizer",
            RuntimeComponentKind.Backend => "Backend",
            _ => Thrower.InvalidOpEx<string>($"Unsupported runtime component kind value '{kind}'.")
        };
    }

    private static RuntimeComponentKind ThrowUnsupported(string value, string? sourceName)
    {
        var sourceSuffix = string.IsNullOrWhiteSpace(sourceName) ? string.Empty : $" in '{sourceName}'";
        return Thrower.InvalidOpEx<RuntimeComponentKind>($"Unsupported runtime component kind '{value}'{sourceSuffix}.");
    }
}
