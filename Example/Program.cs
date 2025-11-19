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
    let a = 5
                   let b = 3  
                   let c = 8
                   let d = 1
                   let e = 2
                   let temp = 0
                   let swapped = 1
                   let i = 0
                   
                   @outer_loop:
                   if swapped == 1 goto @end
                       Main.Print(a)
                       Main.Print(b)
                       Main.Print(c)
                       Main.Print(d)
                       Main.Print(e)
                       Main.Print(0)
                       swapped = 0
                       
                       if a > b
                           temp = a
                           a = b
                           b = temp
                           swapped = 1
                       
                       if b > c
                           temp = b
                           b = c
                           c = temp
                           swapped = 1
                       
                       if c > d
                           temp = c
                           c = d
                           d = temp
                           swapped = 1
                       
                       if d > e
                           temp = d
                           d = e
                           e = temp
                           swapped = 1
                       
                       goto @outer_loop
                   @end:
                   
                   a + b + c + d + e
    """
);

Console.WriteLine(result);