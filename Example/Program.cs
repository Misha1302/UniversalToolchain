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


// TODO: basic type-resolving
// TODO: fix parser configuration
var result = core.Execute(
    """
    a : RealNumberImpl = -2
    @label:
        Main.Print(a)
        a = a + 1
        
        if a < 5 or a == 5 (
            goto @label
        )
    """
);

Console.WriteLine(result);