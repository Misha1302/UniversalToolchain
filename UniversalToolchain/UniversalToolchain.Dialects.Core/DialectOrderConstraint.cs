namespace UniversalToolchain.Dialects.Core;

internal enum DialectOrderConstraintKind
{
    Before,
    After,
    Requires,
}

internal readonly record struct DialectOrderConstraint(
    DialectOrderConstraintKind Kind,
    string SourceModule,
    string TargetModule);
