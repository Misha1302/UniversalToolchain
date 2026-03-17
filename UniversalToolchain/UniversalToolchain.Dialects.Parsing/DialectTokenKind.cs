namespace UniversalToolchain.Dialects.Parsing;

internal enum DialectTokenKind
{
    Identifier = 0,
    StringLiteral = 1,
    Arrow = 2,
    Equals = 3,
    NewLine = 4,
    EndOfInput = 5
}
