// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.LexerWrapper;

namespace BasicCore.ParserWrapper;

public record AstNode(
    AstNodeType NodeType,
    LexemeValue? LexemeValue,
    AstNode? Parent,
    List<AstNode> Children,
    HashSet<string>? Tags = null)
{
    public readonly HashSet<string> Tags = Tags ?? [];
    public AstNodeType NodeType = NodeType;

    public LexemeType? LexemeType => LexemeValue?.LexemePattern.LexemeType;
    public string Text => LexemeValue?.Text ?? "";

    public override string ToString()
    {
        return ToStringCustom(0);
    }

    private string ToStringCustom(int offset)
    {
        var s = new string(' ', offset);
        return
            $"{s}{NodeType}: {LexemeValue} : [\n{string.Join("\n", Children.Select(x => x.ToStringCustom(offset + 4)))}\n{s}]";
    }
}