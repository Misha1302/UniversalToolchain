namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstLineReader
{
    public static IReadOnlyList<AstNode> ReadStatements(AstNode astRoot, DialectDslRegistry registry)
    {
        if (astRoot == null)
            Thrower.ArgumentNull(nameof(astRoot));

        if (registry == null)
            Thrower.ArgumentNull(nameof(registry));

        return DialectDslAstValidator.Validate(astRoot, registry).Children.ToList();
    }
}