using System.Reflection;
using ExceptionsManager;

namespace Tests.Internal;

[TestFixture]
public class BasicCoreOrchestrationInternalBehaviorTests
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
        core.PrepareToRun("value");
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

    [Test]
    public void PrepareToRun_UsesExecutorInitializedByMiddleEndModule()
    {
        var calls = new List<string>();
        var executor = new ConfigurableExecutor(calls);
        var core = new BasicCoreImpl<string>(
            () => new TrackingLexer(calls),
            () => new TrackingParser(calls),
            () => new TrackingAstTranslator(calls),
            () => new TrackingMethodsTranslator(calls),
            () => new TrackingCompiler(calls),
            () => executor,
            [new TrackingFrontendModule(calls)],
            [new TrackingOptimizer(calls)],
            [new ConfiguringMiddleEnd(calls, "initialized")]);

        core.PrepareToRun("value");
        var runResult = core.RunPrepared();

        Assert.That(runResult, Is.EqualTo("initialized:compiled:value"));
        Assert.That(calls.Count(call => call == "configMiddle.InitExecutor"), Is.EqualTo(1));
        Assert.That(calls, Does.Contain("configMiddle.InitExecutor"));
        Assert.That(calls, Does.Contain("configExecutor.Execute"));
        Assert.That(calls.IndexOf("configMiddle.InitExecutor"), Is.LessThan(calls.IndexOf("configExecutor.Execute")));
    }

    [Test]
    public void Compile_DoesNotBypassMiddleEndExecutorInitializationSemantics()
    {
        var calls = new List<string>();
        var executor = new ConfigurableExecutor(calls);
        var core = CreateConfigurableCore(calls, executor);

        var artifact = core.Compile("value");
        var runResult = artifact.CreateSession().Run();

        Assert.That(runResult, Is.EqualTo("initialized:compiled:value"));
        Assert.That(calls.Count(call => call == "configMiddle.InitExecutor"), Is.EqualTo(1));
        Assert.That(calls.IndexOf("configMiddle.InitExecutor"), Is.LessThan(calls.IndexOf("configExecutor.Execute")));
    }

    [Test]
    public void Build_And_Compile_ShareSameExecutorInitializationSemantics()
    {
        var calls = new List<string>();
        var executor = new ConfigurableExecutor(calls);
        var core = CreateConfigurableCore(calls, executor);

        var compileResult = core.Compile("value").CreateSession().Run();
        core.PrepareToRun("value");
        var preparedResult = core.RunPrepared();

        Assert.That(compileResult, Is.EqualTo("initialized:compiled:value"));
        Assert.That(preparedResult, Is.EqualTo("initialized:compiled:value"));
        Assert.That(calls.Count(call => call == "configMiddle.InitExecutor"), Is.EqualTo(2));
    }

    [Test]
    public void RunPrepared_UsesPreparedSession_NotAdHocExecution()
    {
        var calls = new List<string>();
        var executor = new ConfigurableExecutor(calls)
        {
            IncludeFirstArgumentInResult = true
        };
        var core = CreateConfigurableCore(calls, executor);

        core.PrepareToRun(
            new CompilationInput
            {
                SourceText = "value",
                ExternalBindings =
                [
                    new ExternalBinding
                    {
                        Name = "x",
                        Type = typeof(int)
                    }
                ]
            });

        var preparedSession = GetPreparedSession(core);
        preparedSession.SetArgument(0, 42);
        var runResult = core.RunPrepared();

        Assert.That(runResult, Is.EqualTo("initialized:compiled:value|arg:42"));
        Assert.That(calls.Count(call => call == "compiler.Compile"), Is.EqualTo(1));
    }

    [Test]
    public void Compile_Then_CreateSession_And_PrepareToRun_ProduceEquivalentExecutionResult()
    {
        var compileCalls = new List<string>();
        var compileExecutor = new ConfigurableExecutor(compileCalls)
        {
            IncludeFirstArgumentInResult = true
        };
        var compileCore = CreateConfigurableCore(compileCalls, compileExecutor);
        var compilationInput = new CompilationInput
        {
            SourceText = "value",
            ExternalBindings =
            [
                new ExternalBinding
                {
                    Name = "x",
                    Type = typeof(int)
                }
            ]
        };

        var compileSession = compileCore.Compile(compilationInput).CreateSession();
        compileSession.SetArgument("x", 77);
        var compileResult = compileSession.Run();

        var prepareCalls = new List<string>();
        var prepareExecutor = new ConfigurableExecutor(prepareCalls)
        {
            IncludeFirstArgumentInResult = true
        };
        var prepareCore = CreateConfigurableCore(prepareCalls, prepareExecutor);
        prepareCore.PrepareToRun(compilationInput);
        var preparedSession = GetPreparedSession(prepareCore);
        preparedSession.SetArgument("x", 77);
        var preparedResult = prepareCore.RunPrepared();

        Assert.That(preparedResult, Is.EqualTo(compileResult));
    }

    private static ICompiledArtifactSession GetPreparedSession(BasicCoreImpl<string> core)
    {
        var preparedField = typeof(BasicCoreImpl<string>).GetField("_prepared", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(preparedField, Is.Not.Null);

        var preparedAsyncLocal = preparedField!.GetValue(core);
        Assert.That(preparedAsyncLocal, Is.Not.Null);

        var valueProperty = preparedAsyncLocal!.GetType().GetProperty("Value");
        Assert.That(valueProperty, Is.Not.Null);

        var prepared = valueProperty!.GetValue(preparedAsyncLocal);
        Assert.That(prepared, Is.Not.Null);

        var sessionProperty = prepared!.GetType().GetProperty("Session");
        Assert.That(sessionProperty, Is.Not.Null);

        var session = sessionProperty!.GetValue(prepared) as ICompiledArtifactSession;
        Assert.That(session, Is.Not.Null);

        return session!;
    }

    private static BasicCoreImpl<string> CreateConfigurableCore(List<string> calls, ConfigurableExecutor executor)
    {
        return new BasicCoreImpl<string>(
            () => new TrackingLexer(calls),
            () => new TrackingParser(calls),
            () => new TrackingAstTranslator(calls),
            () => new TrackingMethodsTranslator(calls),
            () => new TrackingCompiler(calls),
            () => executor,
            [new TrackingFrontendModule(calls)],
            [new TrackingOptimizer(calls)],
            [new ConfiguringMiddleEnd(calls, "initialized")]);
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

    private sealed class ConfigurableExecutor(List<string> calls) : IExecutor<string>
    {
        private string _prefix = "raw";
        public bool IncludeFirstArgumentInResult { get; init; }

        public object Execute(string compilation, IExecutionEnvironment environment)
        {
            calls.Add("configExecutor.Execute");
            if (!IncludeFirstArgumentInResult)
                return _prefix + ":" + compilation;

            return _prefix + ":" + compilation + "|arg:" + environment.GetExternalValue(0);
        }

        public void SetPrefix(string prefix)
        {
            calls.Add("configExecutor.SetPrefix");
            _prefix = prefix;
        }
    }

    private sealed class ConfiguringMiddleEnd(List<string> calls, string prefix) : IMiddleEndCoreModule<string>
    {
        public void InitMethodsCompiler(IAbstractIrCompiler<string> compiler) => calls.Add("configMiddle.InitMethodsCompiler");

        public string ProcessCompilation(string current)
        {
            calls.Add("configMiddle.ProcessCompilation");
            return current;
        }

        public void InitExecutor(IExecutor<string> executor)
        {
            calls.Add("configMiddle.InitExecutor");

            if (executor is ConfigurableExecutor configurableExecutor)
            {
                configurableExecutor.SetPrefix(prefix);
                return;
            }

            Thrower.InvalidOpEx("Executor must be ConfigurableExecutor for this test.");
        }
    }
}