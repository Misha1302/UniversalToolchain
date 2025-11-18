// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BytecodeDynamicMethodsCompiler;
using ConditionsModule;
using EqualityModule;
using IdentifierModule;
using LabelsModule;
using VariablesModule;

var core = new BasicCoreImpl(
    () => new BasicLexerImpl(),
    () => new BasicParserImpl(),
    () => new BasicBytecodeTranslatorImpl(),
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
        new ConditionsModuleImpl(),
        new ComparisonOperations(),
        new BooleanOperations(),
        new ExecutorDebugLoggerImpl(),
        new ParserConfigurationModuleImpl(ActionType.Dump)
    ]
);


// TODO: fix parser configuration
var result = core.Execute(
    """
    let a = Main.Input()
    Main.Print(a)

    let b = -10
    let c = 123 * 4 - 2
    let d = b * c - 5
    b = b + 1
    d = d - 105
    Main.Print(b)
    Main.Print(c)
    Main.Print(d)
    """
);

Console.WriteLine(result);