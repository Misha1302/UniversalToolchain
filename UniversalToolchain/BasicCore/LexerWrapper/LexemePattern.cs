// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace BasicCore.LexerWrapper;

// If somebody disagrees that lexemes shouldn't be parsed using regex-expressions, let him write his own working lexer without it and Antlr4
/// <param name="Pattern">Regex pattern to recognize lexeme</param>
/// <param name="LexemeType">Lexeme type</param>
public record LexemePattern(string Pattern, LexemeType LexemeType);