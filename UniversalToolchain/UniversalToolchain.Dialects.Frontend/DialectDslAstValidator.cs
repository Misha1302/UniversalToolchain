using BasicCore.ParserWrapper;
using CommonExceptions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslAstValidator
{
    public AstNode Validate(AstNode astRoot)
    {
        var root = astRoot.Children.SingleOrDefault() as DialectRootAstNode;
        if (root == null)
            WistThrower.Parser("Dialect parser did not produce a semantic root node.");

        if (root.Directives.OfType<SecurityModeDirectiveAstNode>().Count() > 1)
            WistThrower.Parser("Security directive can be specified only once.");

        foreach (var directive in root.Directives)
        {
            if (directive is UseModulesDirectiveAstNode use && use.ModuleNames.Any(string.IsNullOrWhiteSpace))
                WistThrower.Parser("Use directive contains an empty module name.");

            if (directive is ExcludeModulesDirectiveAstNode exclude && exclude.ModuleNames.Any(string.IsNullOrWhiteSpace))
                WistThrower.Parser("Exclude directive contains an empty module name.");

            if (directive is FrontendOrderDirectiveAstNode frontend && frontend.ModuleNames.Distinct(StringComparer.Ordinal).Count() != frontend.ModuleNames.Count)
                WistThrower.Parser("Requires directive contains duplicated module names.");
        }

        return astRoot;
    }
}
