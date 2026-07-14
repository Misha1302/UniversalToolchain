namespace BasicCore.LexerWrapper;

public class LexerConfiguration(List<LexemeType> lexemesToIgnore)
{
    private List<LexemeType> _lexemesToIgnore = lexemesToIgnore.ArgNotNull();
    private LevelCollection<float, LexemePattern> _patterns = new();

    public IReadOnlyLevelCollection<float, LexemePattern> LevelCollectionPatterns => _patterns;
    public IReadOnlyList<LexemePattern> Patterns => _patterns.SelectMany(static x => x.Value).ToList();
    public List<LexemeType> LexemesToIgnore => _lexemesToIgnore;

    public bool TryAddPattern(LexemePattern pattern, bool ignore = false, float priority = 0)
    {
        pattern = pattern.ArgNotNull();

        var find = Patterns.Any(x => x == pattern);
        if (find)
            return false;

        Thrower.AssertAlways(
            Patterns.All(x => x.Pattern != pattern.Pattern),
            $"{pattern.Pattern} always added (pattern != inserting)"
        );
        Thrower.AssertAlways(
            Patterns.All(x => x.LexemeType.GetName() != pattern.LexemeType.GetName()),
            $"{pattern.LexemeType} always added (pattern != inserting)"
        );

        _patterns[priority].Add(pattern);

        if (ignore)
            LexemesToIgnore.Add(pattern.LexemeType);
        return true;
    }

    public bool TryUncheckedAddPattern(LexemePattern pattern, bool ignore = false, float priority = 0)
    {
        pattern = pattern.ArgNotNull();

        var find = Patterns.Any(x => x == pattern);
        if (find)
            return false;

        _patterns[priority].Add(pattern);

        if (ignore && !LexemesToIgnore.Contains(pattern.LexemeType))
            LexemesToIgnore.Add(pattern.LexemeType);
        return true;
    }

    public void AddPattern(LexemePattern pattern, bool ignore = false, float priority = 0)
    {
        if (!TryAddPattern(pattern, ignore, priority))
            Thrower.InvalidOpEx("Pattern always added");
    }

    /// <summary>
    ///     Captures the current pattern order and ignore flags without exposing the mutable storage.
    /// </summary>
    public IReadOnlyList<LexerPatternRegistration> CreateSnapshot() =>
        _patterns
            .SelectMany(level => level.Value.Select(pattern => new LexerPatternRegistration(
                level.Key,
                pattern,
                LexemesToIgnore.Contains(pattern.LexemeType))))
            .ToArray();

    /// <summary>
    ///     Replaces the complete configuration only after the incoming snapshot has been validated.
    /// </summary>
    public void ReplaceWith(IEnumerable<LexerPatternRegistration> registrations)
    {
        registrations = registrations.ArgNotNull();

        var snapshot = registrations
            .Select(static item => item.ArgNotNull())
            .ToArray();
        var validatedPatterns = new LevelCollection<float, LexemePattern>();
        var validatedIgnoredLexemes = new List<LexemeType>();
        var seenPatterns = new HashSet<LexemePattern>();
        var seenPatternTexts = new HashSet<string>(StringComparer.Ordinal);
        var seenLexemeTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var registration in snapshot)
        {
            registration.Pattern.ArgNotNull();
            Thrower.AssertAlways(float.IsFinite(registration.Priority), "Lexer pattern priority must be finite.");
            Thrower.AssertAlways(
                seenPatterns.Add(registration.Pattern),
                $"Lexer pattern '{registration.Pattern.Pattern}' for '{registration.Pattern.LexemeType}' is registered more than once.");
            Thrower.AssertAlways(
                seenPatternTexts.Add(registration.Pattern.Pattern),
                $"Lexer regex '{registration.Pattern.Pattern}' is assigned to more than one lexeme type.");
            Thrower.AssertAlways(
                seenLexemeTypeNames.Add(registration.Pattern.LexemeType.GetName()),
                $"Lexeme type '{registration.Pattern.LexemeType}' is assigned more than one regex.");

            validatedPatterns[registration.Priority].Add(registration.Pattern);
            if (registration.Ignore)
                validatedIgnoredLexemes.Add(registration.Pattern.LexemeType);
        }

        // Publish the fully prepared snapshot by replacing backing references. No live state is cleared first.
        _patterns = validatedPatterns;
        _lexemesToIgnore = validatedIgnoredLexemes;
    }
}
