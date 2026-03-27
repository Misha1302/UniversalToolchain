using System;

namespace UniversalToolchain.Dialects.Abstractions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DialectRuntimeExportAttribute : Attribute
{
    public DialectRuntimeExportAttribute(
        string dialectFamily,
        string componentKind,
        string canonicalAlias)
    {
        DialectFamily = dialectFamily;
        ComponentKind = componentKind;
        CanonicalAlias = canonicalAlias;
    }

    public string DialectFamily { get; }

    public string ComponentKind { get; }

    public string CanonicalAlias { get; }
}
