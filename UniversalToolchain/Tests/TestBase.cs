// BasicCore.Tests/TestBase.cs

using BasicStdLib;
using IntermediateRepresentationAbstractions;

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
        IFrontendCoreModule[]? modules = null,
        Dictionary<Type, object>? middleEndModules = null
    )
    {
        modules ??= [];
        middleEndModules ??= [];

        return
        [
            new BasicCoreImpl<DynamicMethod>(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicBytecodeTranslatorImpl(),
                () => new AbstractMethodsCompilerImpl(),
                () => new DynamicMethodExecutor(),
                modules,
                middleEndModules.TryGetValue(typeof(DynamicMethod), out var dmModules)
                    ? (List<IMiddleEndCoreModule<DynamicMethod>>)dmModules
                    : []
            ),
            new BasicCoreImpl<IAbstractIR>(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicBytecodeTranslatorImpl(),
                () => new AbstractMethodsStubImpl(),
                () => new InterpreterImpl(),
                modules,
                middleEndModules.TryGetValue(typeof(IAbstractIR), out var airModules)
                    ? (List<IMiddleEndCoreModule<IAbstractIR>>)airModules
                    : []
            )
        ];
    }

    protected object ExecuteCode(
        string code,
        IFrontendCoreModule[]? modules = null,
        Dictionary<Type, object>? middleEndModules = null
    )
    {
        modules ??= [];
        middleEndModules ??= [];

        var values = CreateCores(modules, middleEndModules)
            .Select(core => core.Run(code))
            .ToList();

        Thrower.AssertAlways(values.All(value => value?.Equals(values[0]) ?? value == values[0]));

        return values[0]!;
    }
}