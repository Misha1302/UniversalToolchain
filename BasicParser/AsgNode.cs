// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicLexer;

namespace BasicParser;

public record AsgNode(
    AsgNodeType NodeType,
    LexemeValue? LexemeValue,
    List<AsgNode> Children,
    int BaseLineNumber = -1)
{
    public readonly AsgNodeType NodeType = NodeType;

    public ExtensibleEnum<LexemeTag>? LexemeType => LexemeValue?.LexemePattern.LexemeType;
    public string Text => LexemeValue?.Text ?? "";
    public int LineNumber => BaseLineNumber == -1 ? LexemeValue!.LineNumber : BaseLineNumber;

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