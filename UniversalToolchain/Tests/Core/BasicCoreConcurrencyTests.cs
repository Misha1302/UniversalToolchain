using BasicCore.Contracts;
using BasicCore.TranslatorWrapper;
using BasicCore.Compilation;
using BasicCore.Core;
using BasicCore.Execution;
using BasicCore.ExecutorWrapper;

namespace Tests.Core;

[TestFixture]
public class BasicCoreConcurrencyTests
{
    [Test]
    public void SharedCore_PrepareToRunAndRunPrepared_ConcurrentCalls_DoNotMixPreparedState()
    {
        var core = CreateCore();
        var firstPrepared = new ManualResetEventSlim(false);
        var secondPrepared = new ManualResetEventSlim(false);
        var runBarrier = new Barrier(2);

        string? firstResult = null;
        string? secondResult = null;

        var firstTask = Task.Run(() =>
        {
            core.PrepareToRun("program-A");
            firstPrepared.Set();
            secondPrepared.Wait();
            runBarrier.SignalAndWait();
            firstResult = core.RunPrepared() as string;
        });

        var secondTask = Task.Run(() =>
        {
            firstPrepared.Wait();
            core.PrepareToRun("program-B");
            secondPrepared.Set();
            runBarrier.SignalAndWait();
            secondResult = core.RunPrepared() as string;
        });

        Task.WaitAll(firstTask, secondTask);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.EqualTo("program-A"),
                "Each concurrent caller should receive result from its own prepared program.");
            Assert.That(secondResult, Is.EqualTo("program-B"),
                "Each concurrent caller should receive result from its own prepared program.");
        });
    }

    private static BasicCoreImpl<string> CreateCore()
    {
        return new BasicCoreImpl<string>(
            () => new PassthroughLexer(),
            () => new PassthroughParser(),
            () => new PassthroughAstTranslator(),
            () => new PassthroughMethodsTranslator(),
            () => new PassthroughCompiler(),
            () => new PassthroughExecutor(),
            [],
            [],
            []);
    }

    private sealed class PassthroughLexer : ILexer
    {
        public LexerConfiguration Configuration { get; } = new([]);

        public List<LexemeValue> Lexemize(string code) => [new(code, null, -1, null)];
    }

    private sealed class PassthroughParser : IParser
    {
        public ParserConfiguration Configuration { get; } = new(new LevelCollection<float, IAstNodeCreator>());

        public AstNode Parse(List<LexemeValue> lexemes) => new(ExtensibleEnum<AstNodeTag>.CreateOrGet("Root"), null, []);

        public void ParseScope(AstNode scope, List<IAstNodeCreator> creators, Predicate<AstNode> needToVisit)
        {
        }
    }

    private sealed class PassthroughAstTranslator : IAstToBytecodeTranslator
    {
        public BytecodeTranslatorConfiguration Configuration { get; } = new([]);

        public Bytecode Translate(AstNode root) => new([]);
    }

    private sealed class PassthroughMethodsTranslator : IAbstractMethodsTranslator
    {
        public IAbstractIR Translate(Bytecode bytecode) => new AbstractIR();
    }

    private sealed class PassthroughCompiler : IAbstractIrCompiler<string>
    {
        public string Compile(IAbstractIR air, CompilationInput input) => input.SourceText;
    }

    private sealed class PassthroughExecutor : IExecutor<string>
    {
        public object Execute(string compilation, IExecutionEnvironment environment) => compilation;
    }
}
