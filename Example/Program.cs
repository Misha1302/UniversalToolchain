// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BytecodeDynamicMethodsCompiler;
using EqualityModule;
using IdentifierModule;
using LabelsModule;
using VariablesModule;

var core = new BasicCoreImpl(
    () => new BasicLexerImpl(),
    () => new BasicParserImpl(),
    () => new BasicBytecodeBytecodeTranslatorImpl(),
    () => new BytecodeDynamicMethodsCompilerImpl(),
    () => new BasicInterpreterImpl(),
    [
        new IdentifierModuleImpl(),
        new ScopesModuleImpl(),
        new NumbersModuleImpl(),
        new WhitespaceModuleImpl(),
        new ArithmeticModuleImpl(),
        new CSharpInteropModuleImpl(),
        new LabelsModuleImpl(),
        new VariablesModuleImpl(),
        new EqualityModuleImpl(),
        new ExecutorDebugLoggerImpl(),
        new ParserConfigurationModuleImpl(ActionType.Dump)
    ]
);


var result = core.Execute(
    """
    a : RealNumberImpl = -5
    @label:
        a = a + 1
        Main.Print(a)
    goto @label
    """
);

Console.WriteLine(result);