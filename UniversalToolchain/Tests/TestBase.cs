// BasicCore.Tests/TestBase.cs

using System.Reflection.Emit;
using BasicCilCompiler;
using BasicCodeTranslator;
using BasicCore;
using BasicCore.ExecutorWrapper;
using BasicLexer;
using BasicParser;
using BytecodeDynamicMethodsCompiler;
using ExceptionsManager;

namespace Tests;

[TestFixture]
public abstract class TestBase
{
    // TODO: remade to others compilations
    protected readonly List<Func<IExecutor<DynamicMethod>>> Executors =
        [() => new DynamicMethodExecutor()];
    // , () => new BasicInterpreterImpl()

    protected IEnumerable<BasicCoreImpl<DynamicMethod>> CreateCores(params IFrontendCoreModule[] modules)
    {
        return Executors.Select(executorFactory => new BasicCoreImpl<DynamicMethod>(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicBytecodeTranslatorImpl(),
                () => new AbstractMethodsCompilerImpl(),
                executorFactory,
                modules,
                []
            )
        );
    }

    protected object ExecuteCode(string code, params IFrontendCoreModule[] modules)
    {
        var values = CreateCores(modules).Select(core => core.Execute(code)).ToList();
        Thrower.AssertAlways(values.All(value => value.Equals(values[0])));
        return values[0];
    }
}