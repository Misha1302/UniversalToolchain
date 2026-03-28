namespace BasicCore.LexerWrapper;

public class LexemeValue
{
    private readonly Lazy<(int, int)> _lineAndChar;

    public readonly LexemePattern? LexemePattern;
    public readonly int StartIndex;
    public readonly string Text;

    public LexemeValue(string text, LexemePattern? lexemePattern, int startIndex, string? code)
    {
        Text = text;
        LexemePattern = lexemePattern;
        StartIndex = startIndex;

        if (StartIndex >= 0 && code != null)
            _lineAndChar = new Lazy<(int, int)>(() => CalcLineAndCharNums(code, StartIndex));
        else _lineAndChar = new Lazy<(int, int)>(() => (-1, -1));
    }

    public int LineNumber => _lineAndChar.Value.Item1;
    public int CharNumber => _lineAndChar.Value.Item2;

    private static (int, int) CalcLineAndCharNums(string code, int startIndex)
    {
        var codePart = string.Join("", code.Take(startIndex));
        var lineNumber = codePart.Count(c => c == '\n') + 1;
        var charNumber = codePart.Split("\n")[^1].Length;
        return (lineNumber, charNumber);
    }

    public override string ToString()
    {
        if (LexemePattern != null)
            return
                $"{Regex.Escape(Text)} ({LexemePattern.LexemeType}:\"{LexemePattern.Pattern}\") at {LineNumber}:{CharNumber}";
        return Regex.Escape(Text);
    }
}