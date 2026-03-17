namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Backend target scope for dialect policies and build plans.
/// </summary>
public enum DialectBackendTarget
{
    Interpreter = 0,
    Cil = 1,
    Any = 2
}
