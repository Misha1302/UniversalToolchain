using BasicCore.LexerWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveLineParser
{
    private readonly DialectDslRegistry _registry;

    public DialectDirectiveLineParser(DialectDslRegistry? registry = null)
    {
        _registry = registry ?? DialectDslBuiltInFeatures.CreateRegistry();
    }

    public bool TryParse(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        if (line == null)
        {
            Thrower.ArgumentNull(nameof(line));
        }

        if (accumulation == null)
        {
            Thrower.ArgumentNull(nameof(accumulation));
        }

        if (line.Count == 0)
        {
            return true;
        }

        var keyword = line[0].Text;
        if (!_registry.TryGetFeature(keyword, out var feature))
        {
            return false;
        }

        feature.Accumulate(line, accumulation);
        return true;
    }
}
