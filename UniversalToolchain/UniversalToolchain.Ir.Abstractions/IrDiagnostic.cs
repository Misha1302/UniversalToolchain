namespace UniversalToolchain.Ir.Abstractions;

public sealed record IrDiagnostic(
    IrDiagnosticSeverity Severity,
    string Code,
    string Message);
