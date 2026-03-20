namespace UniversalToolchain.Dialects.Abstractions;

public sealed class DialectBackendAliasAttribute : DialectAliasAttributeBase
{
    public DialectBackendAliasAttribute(params string[] aliases)
        : base(aliases)
    {
    }
}
