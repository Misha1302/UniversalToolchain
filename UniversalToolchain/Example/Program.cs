using System.Diagnostics;
using System.Reflection.Emit;
using AbstractIrConverters;
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
    () => new BasicAstToBytecodeTranslatorImpl(),
    () => new BytecodeToAbstractIrConverterImpl(),
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

core.PrepareToRun(
    """
    let sum = 0
    let i = 1
    @loop:
    if i > 1000000 goto @end
        sum = sum + i
        i = i + 1
        goto @loop
    @end:
    sum
    """
);

var sw = Stopwatch.StartNew();
var result = core.RunPrepared();
Console.WriteLine(sw.ElapsedMilliseconds);


Console.WriteLine(result);