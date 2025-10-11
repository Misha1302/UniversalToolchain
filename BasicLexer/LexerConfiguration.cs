namespace BasicLexer;

public record LexerConfiguration(List<LexemePattern> Patterns, List<LexemeType> LexemesToIgnore);