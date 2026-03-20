namespace UniversalToolchain.Dialects.Abstractions;

public sealed class DialectModuleAliasAttribute : DialectAliasAttributeBase
{
    public DialectModuleAliasAttribute(params string[] aliases)
        : base(aliases)
    {
    }
}
