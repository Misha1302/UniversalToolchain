// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

var core = new BasicCoreImpl(
    () => new BasicLexerImpl(),
    () => new BasicParserImpl(),
    () => new BasicBytecodeBytecodeTranslatorImpl(),
    () => new BasicInterpreterImpl(),
    [
        new ScopesModuleImpl(), new NumbersModuleImpl(), new WhitespaceModuleImpl(), new ArithmeticModuleImpl(),
        new ExecutorDebugLogger()
    ]
);

var result = core.Execute(
    """
    (1 + 9 - 4 * 2 / (2 + 1) - 7)
    * 
    (1 / (1 / 3))
    """
);

Console.WriteLine(result);