// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using ExceptionsManager;

namespace BasicCore.LexerWrapper;

public record LexerConfiguration(List<LexemePattern> Patterns, List<LexemeType> LexemesToIgnore)
{
    public bool TryAddPattern(LexemePattern pattern, bool ignore = false, bool insertToStart = false)
    {
        var find = Patterns.Any(x => x == pattern);
        if (find) return false;

        Thrower.AssertAlways(
            Patterns.All(x => x.Pattern != pattern.Pattern),
            $"{pattern.Pattern} always added"
        );
        Thrower.AssertAlways(
            Patterns.All(x => x.LexemeType.GetName() != pattern.LexemeType.GetName()),
            $"{pattern.LexemeType} always added"
        );

        if (!insertToStart) Patterns.Add(pattern);
        else Patterns.Insert(0, pattern);
        if (ignore) LexemesToIgnore.Add(pattern.LexemeType);
        return true;
    }
}