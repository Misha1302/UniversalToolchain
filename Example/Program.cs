// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using LabelsModule;

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
        new LabelsModuleImpl(),
        new ParserConfigurationModuleImpl(ActionType.Dump)
    ]
);

var result = core.Execute(
    """
    label:
        Main.Print(Math.Sqrt(5) * Math.Sqrt(5 * 5 * 5))
    goto label
    """
);

Console.WriteLine(result);