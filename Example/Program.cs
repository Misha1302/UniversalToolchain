// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BytecodeDynamicMethodsCompiler;
using ConditionsModule;
using EqualityModule;
using IdentifierModule;
using LabelsModule;
using SemicolonAsNewLineModule;
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
        new SemicolonAsNewLineModuleImpl(),
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
    let a = 1
    let b = -3
    let c = 2
    let discriminant = b * b - 4 * a * c
    let root1 = (-b + discriminant) / (2 * a)
    let root2 = (-b - discriminant) / (2 * a)
    root1 + root2
    """
);

Console.WriteLine(result);