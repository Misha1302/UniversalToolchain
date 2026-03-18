using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public enum DialectDirectiveKind
{
    UseModules,
    ExcludeModules,
    RequiresModules,
    BeforeModules,
    AfterModules,
    Backend,
    AllowIntrinsic,
    ForbidIntrinsic,
    EnableIntrinsic,
    DisableIntrinsic,
    Security,
    Capability
}

public enum DialectDirectiveArgumentShape
{
    Identifier,
    IdentifierList
}

public sealed class DialectDirectiveDescriptor
{
    public required DialectDirectiveKind Kind { get; init; }

    public required string Keyword { get; init; }

    public required DialectDirectiveArgumentShape ArgumentShape { get; init; }

    public bool IsSingleton { get; init; }
}

public static class DialectDirectiveDescriptors
{
    private static readonly IReadOnlyList<DialectDirectiveDescriptor> _orderedDescriptors =
    [
        new() { Kind = DialectDirectiveKind.UseModules, Keyword = "use", ArgumentShape = DialectDirectiveArgumentShape.IdentifierList },
        new() { Kind = DialectDirectiveKind.ExcludeModules, Keyword = "exclude", ArgumentShape = DialectDirectiveArgumentShape.IdentifierList },
        new() { Kind = DialectDirectiveKind.RequiresModules, Keyword = "requires", ArgumentShape = DialectDirectiveArgumentShape.IdentifierList },
        new() { Kind = DialectDirectiveKind.BeforeModules, Keyword = "before", ArgumentShape = DialectDirectiveArgumentShape.IdentifierList },
        new() { Kind = DialectDirectiveKind.AfterModules, Keyword = "after", ArgumentShape = DialectDirectiveArgumentShape.IdentifierList },
        new() { Kind = DialectDirectiveKind.Backend, Keyword = "backend", ArgumentShape = DialectDirectiveArgumentShape.IdentifierList },
        new() { Kind = DialectDirectiveKind.AllowIntrinsic, Keyword = "allow", ArgumentShape = DialectDirectiveArgumentShape.Identifier },
        new() { Kind = DialectDirectiveKind.ForbidIntrinsic, Keyword = "forbid", ArgumentShape = DialectDirectiveArgumentShape.Identifier },
        new() { Kind = DialectDirectiveKind.EnableIntrinsic, Keyword = "enable", ArgumentShape = DialectDirectiveArgumentShape.Identifier },
        new() { Kind = DialectDirectiveKind.DisableIntrinsic, Keyword = "disable", ArgumentShape = DialectDirectiveArgumentShape.Identifier },
        new() { Kind = DialectDirectiveKind.Security, Keyword = "security", ArgumentShape = DialectDirectiveArgumentShape.Identifier, IsSingleton = true },
        new() { Kind = DialectDirectiveKind.Capability, Keyword = "capability", ArgumentShape = DialectDirectiveArgumentShape.IdentifierList }
    ];

    private static readonly IReadOnlyDictionary<DialectDirectiveKind, DialectDirectiveDescriptor> _byKind =
        _orderedDescriptors.ToDictionary(x => x.Kind);

    private static readonly IReadOnlyDictionary<string, DialectDirectiveDescriptor> _byKeyword =
        _orderedDescriptors.ToDictionary(x => x.Keyword, StringComparer.Ordinal);

    public static IReadOnlyList<DialectDirectiveDescriptor> Ordered => _orderedDescriptors;

    public static DialectDirectiveDescriptor Get(DialectDirectiveKind kind)
    {
        if (!_byKind.TryGetValue(kind, out var descriptor))
        {
            Thrower.Argument(nameof(kind), $"Unknown dialect directive kind '{kind}'.");
        }

        return descriptor;
    }

    public static bool TryGetByKeyword(string keyword, out DialectDirectiveDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            Thrower.Argument(nameof(keyword), "Directive keyword must not be empty.");
        }

        return _byKeyword.TryGetValue(keyword, out descriptor!);
    }
}
