// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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
                : ActionType.Dump)
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