namespace UniversalToolchain.Dialects.Abstractions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DialectRuntimeAliasAttribute : Attribute
{
    public DialectRuntimeAliasAttribute(string alias)
    {
        Alias = alias;
    }

    public string Alias { get; }
}