namespace UniversalToolchain.Dialects.Abstractions;

public sealed class DialectOptimizerAliasAttribute : DialectAliasAttributeBase
{
    public DialectOptimizerAliasAttribute(params string[] aliases)
        : base(aliases)
    {
    }
}
