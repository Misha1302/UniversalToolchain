namespace UniversalToolchain.Dialects.Parsing;

internal readonly record struct DialectToken(DialectTokenKind Kind, string Text, int Line, int Column);
