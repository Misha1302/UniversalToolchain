// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

var core = new BasicCoreImpl(
    () => new BasicLexerImpl(),
    () => new BasicParserImpl(),
    () => new BasicBytecodeBytecodeTranslatorImpl(),
    () => new BasicInterpreterImpl(),
    [
        new ScopesModuleImpl(),
        new NumbersModuleImpl(),
        new WhitespaceModuleImpl(),
        new ArithmeticModuleImpl(),
        new ExecutorDebugLogger(),
        new CSharpInteropModuleImpl(),
        new ParserConfigurationModuleImpl(ActionType.Dump)
    ]
);

var result = core.Execute(
    """
    Main.Print(Math.Sqrt(5) * 1.5 + 2)
    """
);

Console.WriteLine(result);