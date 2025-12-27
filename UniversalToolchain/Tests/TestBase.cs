// BasicCore.Tests/TestBase.cs

using AbstractIrConverters;
using BasicStdLib;
using IntermediateRepresentationAbstractions;
using LocalVariablesOptimizerModule;

namespace Tests;

[TestFixture]
public abstract class TestBase
{
    protected const int CoresCount = 2;

    protected TestBase()
    {
        Main.LoadStdLibToThisAssembly();
    }

    private static IEnumerable<ICoreRunnable> CreateCores(
        Dictionary<Type, object>? middleEndModules = null
    )
    {
        var modules = CreateDefaultModules();
        middleEndModules ??= [];

        return
        [
            new BasicCoreImpl<DynamicMethod>(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicAstToBytecodeTranslatorImpl(),
                () => new BytecodeToAbstractIrConverterImpl(),
                () => new AbstractMethodsCompilerImpl(),
                () => new DynamicMethodExecutor(),
                modules.Union(CreateOptimizers()).ToList(),
                middleEndModules.TryGetValue(typeof(DynamicMethod), out var dmModules)
                    ? (List<IMiddleEndCoreModule<DynamicMethod>>)dmModules
                    : []
            ),
            new BasicCoreImpl<IAbstractIR>(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicAstToBytecodeTranslatorImpl(),
                () => new BytecodeToAbstractIrConverterImpl(),
                () => new AbstractIrToAbstractIrStub(),
                () => new InterpreterImpl(),
                modules,
                middleEndModules.TryGetValue(typeof(IAbstractIR), out var airModules)
                    ? (List<IMiddleEndCoreModule<IAbstractIR>>)airModules
                    : []
            )
        ];
    }

    private static List<IFrontendCoreModule> CreateDefaultModules()
    {
        return
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
            new BooleanOperations()
        ];
    }

    private static List<IFrontendCoreModule> CreateOptimizers()
    {
        return [new LocalVariablesOptimizer()];
    }

    protected object ExecuteCode(
        string code,
        Dictionary<Type, object>? middleEndModules = null
    )
    {
        middleEndModules ??= [];

        var values = CreateCores(middleEndModules)
            .Select(core => core.Run(code))
            .ToList();

        Thrower.AssertAlways(values.All(value => value?.Equals(values[0]) ?? value == values[0]));

        return values[0]!;
    }
}