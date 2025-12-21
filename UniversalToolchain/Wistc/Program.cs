Parser.Default.ParseArguments<Options>(args).WithParsed(Main);

return;

void Main(Options options)
{
    Thrower.AssertAlways(options.SourcePath != null, "SourcePath is required");


    var modules = (List<ICoreModule>)
    [
        new IdentifierModuleImpl(),
        new ScopesModuleImpl(),
        new NumbersModuleImpl(),
        new WhitespaceModuleImpl(),
        new ArithmeticModuleImpl(),
        new CSharpInteropModuleImpl(),
        new LabelsModuleImpl(),
        new VariablesModuleImpl(),
        new EqualityModuleImpl()
    ];

    if (options.LogsPath != null)
        modules.Add(new ExecutorDebugLoggerImpl(options.LogsPath));

    if (options.ParserConfigurationPath != null)
        modules.Add(new ParserConfigurationModuleImpl(
            options.NeedToReadParserConfiguration
                ? ActionType.ReadConfiguration
                : ActionType.DumpConfiguration)
        );


    var core = new BasicCoreImpl(
        () => new BasicLexerImpl(),
        () => new BasicParserImpl(),
        () => new BasicBytecodeTranslatorImpl(),
        () => new BytecodeDynamicMethodsCompilerImpl(),
        () => new BasicInterpreterImpl(),
        modules
    );

    var code = File.ReadAllText(options.SourcePath);
    var result = core.Execute(code);
    Console.WriteLine(result);
}