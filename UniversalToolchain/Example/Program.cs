using System.Reflection.Emit;
using BasicCilCompiler;
using BytecodeDynamicMethodsCompiler;
using ConditionsModule;
using EqualityModule;
using IdentifierModule;
using LabelsModule;
using SemicolonAsNewLineModule;
using VariablesModule;

var core = new BasicCoreImpl<DynamicMethod>(
    () => new BasicLexerImpl(),
    () => new BasicParserImpl(),
    () => new BasicBytecodeTranslatorImpl(),
    () => new AbstractMethodsCompilerImpl(),
    () => new DynamicMethodExecutor(),
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
        new ParserConfigurationModuleImpl(ActionType.DumpConfiguration),
        new LexerConfigurationModuleImpl(ActionType.DumpConfiguration)
    ],
    []
);

var result = core.Execute(
    """
    let a = 5
    a
    """
);

Console.WriteLine(result);