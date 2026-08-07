namespace BasicCore.Compilation;

/// <summary>
/// Canonical low-level compiler stages shared by the legacy orchestration path and direct artifact runtimes.
/// This type owns stage mechanics only; it does not select modules, optimizers, backends, or runtime providers.
/// </summary>
public static class CanonicalArtifactStages
{
    public static AstNode ParseAndBind(
        CompilationInput input,
        ILexer lexer,
        IParser parser,
        IReadOnlyList<IFrontendCoreModule> modules)
    {
        input = input.ArgNotNull();
        lexer = lexer.ArgNotNull();
        parser = parser.ArgNotNull();
        modules = modules.ArgNotNull();

        var targetCode = modules.Aggregate(input.SourceText, static (current, module) => module.ProcessText(current));
        foreach (var module in modules)
            module.InitLexer(lexer);
        var lexemes = lexer.Lexemize(targetCode);

        var targetLexemes = modules.Aggregate(lexemes, static (current, module) => module.ProcessLexemes(current));
        foreach (var module in modules)
            module.InitParser(parser);
        var astRoot = parser.Parse(targetLexemes);

        var targetRoot = modules.Aggregate(astRoot, static (current, module) => module.ProcessAst(current));
        var bindingRules = modules.SelectMany(static module => module.GetAstBindingRules()).ToArray();
        return new Binder(input.ExternalBindings, bindingRules).Bind(targetRoot);
    }

    public static Bytecode LowerToBytecode(
        AstNode root,
        IAstToBytecodeTranslator translator,
        IReadOnlyList<IFrontendCoreModule> modules)
    {
        root = root.ArgNotNull();
        translator = translator.ArgNotNull();
        modules = modules.ArgNotNull();

        foreach (var module in modules)
            module.InitAstTranslator(translator, modules);
        var bytecode = translator.Translate(root);
        return modules.Aggregate(bytecode, static (current, module) => module.ProcessBytecode(current));
    }

    public static IAbstractIR LowerToAir(
        Bytecode bytecode,
        IAbstractMethodsTranslator translator)
    {
        bytecode = bytecode.ArgNotNull();
        translator = translator.ArgNotNull();
        return translator.Translate(bytecode);
    }
}
