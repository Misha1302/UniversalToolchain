namespace UniversalToolchain.Capabilities.Abstractions;

public readonly record struct LanguageFeatureId(string Value)
{
    public override string ToString() => Value;
}