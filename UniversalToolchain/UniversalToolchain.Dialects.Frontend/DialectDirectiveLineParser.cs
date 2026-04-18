using BasicCore.LexerWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Frontend.Composition;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveLineParser
{
    private readonly DialectDslRegistry _registry;

    public DialectDirectiveLineParser(DialectDslRegistry registry)
    {
        registry = registry.ArgNotNull();

        _registry = registry;
    }

    public bool TryParse(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        line = line.ArgNotNull();

        accumulation = accumulation.ArgNotNull();

        if (line.Count == 0)
            return true;

        var keyword = line[0].Text;
        if (!_registry.TryGetFeature(keyword, out var feature))
            return false;

        feature.Accumulate(line, accumulation);
        return true;
    }
}