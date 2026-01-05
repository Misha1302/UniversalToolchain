using System.Diagnostics.CodeAnalysis;

namespace BasicCore.LexerWrapper;

// If somebody disagrees that lexemes shouldn't be parsed using regex-expressions, let him write his own working lexer without it and Antlr4
/// <param name="Pattern">Regex pattern to recognize lexeme</param>
/// <param name="LexemeType">Lexeme type</param>
public record LexemePattern([StringSyntax("Regex")] string Pattern, LexemeType LexemeType);