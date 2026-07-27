namespace Tests.Core;

[TestFixture]
public class BasicCoreLifecycleContractsTests
{
    [Test]
    public void FailedPrepareToRun_InvalidatesPreviouslyPreparedProgram()
    {
        var compiler = new ConditionalCompiler();
        var core = new BasicCoreImpl<string>(
            () => new PassthroughLexer(),
            () => new PassthroughParser(),
            () => new PassthroughAstTranslator(),
            () => new PassthroughMethodsTranslator(),
            () => compiler,
            () => new PassthroughExecutor(),
            [],
            [],
            []);

        core.PrepareToRun("program-A");
        Assert.That(core.RunPrepared(), Is.EqualTo("program-A"));

        Assert.Throws<InvalidOperationException>(() => core.PrepareToRun("broken-program-B"));
        Assert.Throws<InvalidOperationException>(() => core.RunPrepared());
    }

    [Test]
    public void Run_DoesNotReplaceExplicitlyPreparedExecution()
    {
        var core = CreateCore();
        core.PrepareToRun("prepared-program");

        Assert.That(core.Run("one-shot-program"), Is.EqualTo("one-shot-program"));
        Assert.That(core.RunPrepared(), Is.EqualTo("prepared-program"));
    }

    [Test]
    public void RunPrepared_WithoutPrepareToRun_ShouldThrowInvalidOperationException()
    {
        var core = CreateCore();

        var ex = Assert.Throws<InvalidOperationException>(() => core.RunPrepared());

        Assert.That(ex, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex!.Message, Is.Not.Empty);
        Assert.That(ex.Message, Does.Contain("Assertion failed"));
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

    private sealed class ConditionalCompiler : IAbstractIrCompiler<string>
    {
        public string Compile(IAbstractIR air, CompilationInput input)
        {
            if (input.SourceText == "broken-program-B")
                throw new InvalidOperationException("compile failed");

            return input.SourceText;
        }
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