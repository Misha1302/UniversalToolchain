// BasicCore.Tests/TestBase.cs

using BasicCilCompiler;
using BasicCodeTranslator;
using BasicCore;
using BasicInterpreter;
using BasicLexer;
using BasicParser;
using BytecodeDynamicMethodsCompiler;

namespace Tests;

[TestFixture]
public abstract class TestBase
{
    protected BasicCoreImpl CreateCore(params ICoreModule[] modules)
    {
        return new BasicCoreImpl(
            () => new BasicLexerImpl(),
            () => new BasicParserImpl(),
            () => new BasicBytecodeTranslatorImpl(),
            () => new BytecodeDynamicMethodsCompilerImpl(),
            () => new BasicInterpreterImpl(),
            modules
        );
    }

    protected object ExecuteCode(string code, params ICoreModule[] modules)
    {
        var core = CreateCore(modules);
        return core.Execute(code);
    }
}