using System;

namespace UniversalToolchain.Dialects.Abstractions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DialectRuntimeExportAttribute : Attribute
{
    public DialectRuntimeExportAttribute(
        string componentKind,
        string canonicalAlias)
    {
        ComponentKind = componentKind;
        CanonicalAlias = canonicalAlias;
    }

    public string ComponentKind { get; }

    public string CanonicalAlias { get; }
}
