using System.Threading;
using BasicCore.LexerWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

/// <summary>
/// Async-local handoff for tokens produced by the framework lexer and consumed by dialect slice compilation.
/// </summary>
public static class DialectCompilationTokenContext
{
    private static readonly AsyncLocal<List<LexemeValue>?> CurrentTokens = new();

    public static void Set(List<LexemeValue> tokens)
    {
        if (tokens == null)
            Thrower.ArgumentNull(nameof(tokens));

        CurrentTokens.Value = tokens;
    }

    public static bool TryTake(out IReadOnlyList<LexemeValue> tokens)
    {
        if (CurrentTokens.Value == null)
        {
            tokens = [];
            return false;
        }

        tokens = CurrentTokens.Value;
        CurrentTokens.Value = null;
        return true;
    }

    public static void Clear()
    {
        CurrentTokens.Value = null;
    }
}
