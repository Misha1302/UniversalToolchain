using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public abstract class DialectAliasAttributeBase : Attribute
{
    private readonly string[] _aliases;

    protected DialectAliasAttributeBase(params string[] aliases)
    {
        if (aliases == null)
            Thrower.ArgumentNull(nameof(aliases));

        if (aliases.Length == 0)
            Thrower.Argument(nameof(aliases), "At least one alias must be declared.");

        _aliases = aliases
            .Select(static x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    Thrower.Argument(nameof(aliases), "Alias metadata must not contain empty values.");

                return x;
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> Aliases => _aliases;
}
