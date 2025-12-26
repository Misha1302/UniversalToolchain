using System.Reflection.Emit;
using BasicCilCompiler;
using BasicStdLib;
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

Main.LoadStdLibToThisAssembly();

var result = core.Run(
    """
    let a = 2.0
    let b = 3.0
    let c = 4.0

    let result = Main.Pow(a, b) + 
               (Main.Sqrt(c) * a) - 
               Main.Log(b, a) 

    result
    """
);

Console.WriteLine(result);