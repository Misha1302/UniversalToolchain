namespace Tests.Infrastructure;

[TestFixture]
public class BasicCoreImplOrchestrationTests
{
    [Test]
    public void Should_RunPipelineInExpectedOrder_When_PrepareToRunIsCalled()
    {
        var calls = new List<string>();
        var lexer = new TrackingLexer(calls);
        var parser = new TrackingParser(calls);
        var translator = new TrackingAstTranslator(calls);
        var methodsTranslator = new TrackingMethodsTranslator(calls);
        var compiler = new TrackingCompiler(calls);
        var executor = new TrackingExecutor(calls);
        var frontendModule = new TrackingFrontendModule(calls);
        var optimizer = new TrackingOptimizer(calls);
        var middle = new TrackingMiddleEnd(calls);

        var core = new BasicCoreImpl<string>(
            () => lexer,
            () => parser,
            () => translator,
            () => methodsTranslator,
            () => compiler,
            () => executor,
            [frontendModule],
            [optimizer],
            [middle]);

        core.PrepareToRun("raw");

        Assert.That(calls, Is.EqualTo(new[]
        {
            "module.ProcessText",
            "module.InitLexer",
            "lexer.Lexemize",
            "module.ProcessLexemes",
            "module.InitParser",
            "parser.Parse",
            "module.ProcessAst",
            "module.InitAstTranslator",
            "translator.Translate",
            "module.ProcessBytecode",
            "optimizer.InitMethodsTranslator",
            "methodsTranslator.Translate",
            "optimizer.ProcessIr",
            "middle.InitMethodsCompiler",
            "compiler.Compile",
            "middle.ProcessCompilation",
            "middle.InitExecutor"
        }));
    }

    [Test]
    public void Should_ReturnExecutableAndRunPreparedConsistently_When_PreparedOnce()
    {
        var calls = new List<string>();
        var compiler = new TrackingCompiler(calls);
        var executor = new TrackingExecutor(calls);
        var core = CreateCore(calls, compiler, executor);

        var executable = core.GetExecutable("value");
        var runResult = core.RunPrepared();

        Assert.That(executable, Is.EqualTo("compiled:value"));
        Assert.That(runResult, Is.EqualTo("exec:compiled:value"));
    }

    [Test]
    public void Should_PropagateStageException_When_ParserFails()
    {
        var calls = new List<string>();
        var parser = new TrackingParser(calls, true);

        var core = new BasicCoreImpl<string>(
            () => new TrackingLexer(calls),
            () => parser,
            () => new TrackingAstTranslator(calls),
            () => new TrackingMethodsTranslator(calls),
            () => new TrackingCompiler(calls),
            () => new TrackingExecutor(calls),
            [new TrackingFrontendModule(calls)],
            [new TrackingOptimizer(calls)],
            [new TrackingMiddleEnd(calls)]);

        var ex = Assert.Throws<InvalidOperationException>(() => core.PrepareToRun("boom"));

        Assert.That(ex!.Message, Is.EqualTo("parse failed"));
    }

    private static BasicCoreImpl<string> CreateCore(List<string> calls, TrackingCompiler compiler, TrackingExecutor executor)
    {
        return new BasicCoreImpl<string>(
            () => new TrackingLexer(calls),
            () => new TrackingParser(calls),
            () => new TrackingAstTranslator(calls),
            () => new TrackingMethodsTranslator(calls),
            () => compiler,
            () => executor,
            [new TrackingFrontendModule(calls)],
            [new TrackingOptimizer(calls)],
            [new TrackingMiddleEnd(calls)]);
    }

    private sealed class TrackingFrontendModule(List<string> calls) : IFrontendCoreModule
    {
        public string ProcessText(string curCode)
        {
            calls.Add("module.ProcessText");
            return curCode + "|module";
        }

        public void InitLexer(ILexer lexer) => calls.Add("module.InitLexer");

        public List<LexemeValue> ProcessLexemes(List<LexemeValue> current)
        {
            calls.Add("module.ProcessLexemes");
            return current;
        }

        public void InitParser(IParser parser) => calls.Add("module.InitParser");

        public AstNode ProcessAst(AstNode astRoot)
        {
            calls.Add("module.ProcessAst");
            return astRoot;
        }

        public void InitAstTranslator(IAstToBytecodeTranslator translator) => calls.Add("module.InitAstTranslator");

        public Bytecode ProcessBytecode(Bytecode current)
        {
            calls.Add("module.ProcessBytecode");
            return current;
        }
    }

    private sealed class TrackingOptimizer(List<string> calls) : IIRProcessingModule
    {
        public void InitMethodsTranslator(IAbstractMethodsTranslator methodsTranslator) => calls.Add("optimizer.InitMethodsTranslator");

        public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
        {
            calls.Add("optimizer.ProcessIr");
            return current;
        }
    }

    private sealed class TrackingMiddleEnd(List<string> calls) : IMiddleEndCoreModule<string>
    {
        public void InitMethodsCompiler(IAbstractIrCompiler<string> compiler) => calls.Add("middle.InitMethodsCompiler");

        public string ProcessCompilation(string current)
        {
            calls.Add("middle.ProcessCompilation");
            return current;
        }

        public void InitExecutor(IExecutor<string> executor) => calls.Add("middle.InitExecutor");
    }

    private sealed class TrackingLexer(List<string> calls) : ILexer
    {
        public LexerConfiguration Configuration { get; } = new([]);

        public List<LexemeValue> Lexemize(string code)
        {
            calls.Add("lexer.Lexemize");
            return [new LexemeValue(code, null, -1, null)];
        }
    }

    private sealed class TrackingParser(List<string> calls, bool shouldThrow = false) : IParser
    {
        public ParserConfiguration Configuration { get; } = new(new LevelCollection<float, IAstNodeCreator>());

        public AstNode Parse(List<LexemeValue> lexemes)
        {
            calls.Add("parser.Parse");
            if (shouldThrow)
                Thrower.InvalidOpEx("parse failed");
            return new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Root"), null, []);
        }

        public void ParseScope(AstNode scope, List<IAstNodeCreator> creators, Predicate<AstNode> needToVisit)
        {
        }
    }

    private sealed class TrackingAstTranslator(List<string> calls) : IAstToBytecodeTranslator
    {
        public BytecodeTranslatorConfiguration Configuration { get; } = new([]);

        public Bytecode Translate(AstNode root)
        {
            calls.Add("translator.Translate");
            return new Bytecode([]);
        }
    }

    private sealed class TrackingMethodsTranslator(List<string> calls) : IAbstractMethodsTranslator
    {
        public IAbstractIR Translate(Bytecode bytecode)
        {
            calls.Add("methodsTranslator.Translate");
            return new AbstractIR();
        }
    }

    private sealed class TrackingCompiler(List<string> calls) : IAbstractIrCompiler<string>
    {
        public string Compile(IAbstractIR air, CompilationInput input)
        {
            calls.Add("compiler.Compile");
            return "compiled:" + input.SourceText;
        }
    }

    private sealed class TrackingExecutor(List<string> calls) : IExecutor<string>
    {
        public object Execute(string compilation, IExecutionEnvironment environment)
        {
            calls.Add("executor.Execute");
            return "exec:" + compilation;
        }
    }
}