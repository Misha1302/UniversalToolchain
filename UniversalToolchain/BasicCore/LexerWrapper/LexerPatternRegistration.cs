namespace BasicCore.LexerWrapper;

/// <summary>
///     Immutable lexer pattern registration used to validate and replace a lexer configuration as one snapshot.
/// </summary>
public sealed record LexerPatternRegistration(float Priority, LexemePattern Pattern, bool Ignore);
