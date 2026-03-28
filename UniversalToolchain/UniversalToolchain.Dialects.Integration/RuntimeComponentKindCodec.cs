using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public static class RuntimeComponentKindCodec
{
    public static RuntimeComponentKind Parse(string value, string source)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(nameof(value), $"Runtime component kind must not be empty in '{source}'.");

        return value.Trim() switch
        {
            "FrontendModule" => RuntimeComponentKind.FrontendModule,
            "Optimizer" => RuntimeComponentKind.Optimizer,
            "Backend" => RuntimeComponentKind.Backend,
            _ => Thrower.InvalidOpEx<RuntimeComponentKind>($"Unsupported runtime component kind '{value}' in '{source}'.")
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
}
