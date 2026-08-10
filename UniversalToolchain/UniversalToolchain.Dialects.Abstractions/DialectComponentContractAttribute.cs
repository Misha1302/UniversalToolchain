namespace UniversalToolchain.Dialects.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DialectComponentContractAttribute : Attribute
{
    public DialectComponentContractAttribute(
        string componentKind,
        string canonicalAlias)
    {
        ComponentKind = componentKind;
        CanonicalAlias = canonicalAlias;
    }

    public string ComponentKind { get; }

    public string CanonicalAlias { get; }
}