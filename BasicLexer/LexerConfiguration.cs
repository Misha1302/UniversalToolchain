// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;

namespace BasicLexer;

public record LexerConfiguration(List<LexemePattern> Patterns, List<LexemeType> LexemesToIgnore);