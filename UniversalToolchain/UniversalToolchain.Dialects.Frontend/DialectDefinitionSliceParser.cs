using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceParser
{
    private readonly DialectDirectiveLineParser _lineParser = new();

    public DialectDefinitionSlice Parse(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        return ParseLines(DialectAstLineReader.ReadLines(astRoot));
    }

    private DialectDefinitionSlice ParseLines(IReadOnlyList<IReadOnlyList<LexemeValue>> lines)
    {
        if (lines.Count == 0)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect source is empty.", null);
        }

        var dialectName = ParseHeader(lines[0]);
        var accumulation = new DialectDirectiveAccumulation();

        foreach (var line in lines.Skip(1))
        {
            if (!_lineParser.TryParse(line, accumulation))
            {
                DialectDefinitionSliceParseErrors.Fail("Unknown directive in dialect source.", line[0]);
            }
        }

        return new DialectDefinitionSlice(
            dialectName,
            accumulation.UseModules,
            accumulation.ExcludeModules,
            accumulation.OrderDirectives,
            accumulation.BackendDirectives,
            accumulation.IntrinsicDirectives,
            accumulation.OptimizerDirectives,
            accumulation.SecurityProfile,
            accumulation.CapabilityDirectives);
    }

    private static string ParseHeader(IReadOnlyList<LexemeValue> headerLine)
    {
        if (headerLine.Count != 2 ||
            !DialectLexemeTags.IsTag(headerLine[0], DialectLexemeTags.DialectKeyword) ||
            !DialectLexemeTags.IsTag(headerLine[1], DialectLexemeTags.Identifier))
        {
            DialectDefinitionSliceParseErrors.Fail("Expected header: dialect <Name>.", headerLine.FirstOrDefault());
        }

        return headerLine[1].Text;
    }
}
