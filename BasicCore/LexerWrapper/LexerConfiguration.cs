// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace BasicCore.LexerWrapper;

public class LexerConfiguration(List<LexemeType> lexemesToIgnore)
{
    private readonly LevelCollection<float, LexemePattern> _patterns = new();

    public IReadOnlyList<LexemePattern> Patterns => _patterns.SelectMany(x => x.Value).ToList();
    public List<LexemeType> LexemesToIgnore { get; } = lexemesToIgnore;

    public bool TryAddPattern(LexemePattern pattern, bool ignore = false, float priority = 0)
    {
        var find = Patterns.Any(x => x == pattern);
        if (find) return false;

        Thrower.AssertAlways(
            Patterns.All(x => x.Pattern != pattern.Pattern),
            $"{pattern.Pattern} always added (pattern != inserting)"
        );
        Thrower.AssertAlways(
            Patterns.All(x => x.LexemeType.GetName() != pattern.LexemeType.GetName()),
            $"{pattern.LexemeType} always added (pattern != inserting)"
        );

        _patterns[priority].Add(pattern);

        if (ignore) LexemesToIgnore.Add(pattern.LexemeType);
        return true;
    }


    public void AddPattern(LexemePattern pattern, bool ignore = false, float priority = 0)
    {
        if (!TryAddPattern(pattern, ignore, priority))
            Thrower.InvalidOpEx("Pattern always added");
    }
}