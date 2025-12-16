// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com
// BasicCore.Tests/TestBase.cs

using BasicCilCompiler;
using BasicCodeTranslator;
using BasicCore;
using BasicCore.ExecutorWrapper;
using BasicInterpreter;
using BasicLexer;
using BasicParser;
using BytecodeDynamicMethodsCompiler;
using ExceptionsManager;

namespace Tests;

[TestFixture]
public abstract class TestBase
{
    protected readonly List<Func<IExecutor>> Executors =
        [() => new BasicCilCompilerImpl(), () => new BasicInterpreterImpl()];

    protected IEnumerable<BasicCoreImpl> CreateCores(params ICoreModule[] modules)
    {
        return Executors.Select(createExecutor => new BasicCoreImpl(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicBytecodeTranslatorImpl(),
                () => new BytecodeDynamicMethodsCompilerImpl(),
                createExecutor,
                modules
            )
        );
    }

    protected object ExecuteCode(string code, params ICoreModule[] modules)
    {
        var values = CreateCores(modules).Select(core => core.Execute(code)).ToList();
        Thrower.AssertAlways(values.All(value => value.Equals(values[0])));
        return values[0];
    }
}