using BasicCore.Contracts;
using BasicCore.TranslatorWrapper;
using BasicCore.Compilation;
using BasicCore.Core;
using BasicCore.Execution;
using BasicCore.ExecutorWrapper;

namespace Tests.Core;

[TestFixture]
public class CompilationInputNullRuntimeBugTests
{
    [Test]
    public void NormalizeRuntimeInput_WithNullValue_DoesNotThrow()
    {
        var normalizer = new CompilationInputNormalizer();
        var parameters = new Dictionary<string, object> { ["x"] = null! };

        Assert.DoesNotThrow(() => normalizer.NormalizeRuntimeInput("x", parameters),
            "Runtime input normalization should tolerate null external values.");
    }

    [Test]
    public void Run_WithNullExternalParameter_DoesNotThrow()
    {
        var core = CreateCore();
        var parameters = new Dictionary<string, object> { ["x"] = null! };

        Assert.DoesNotThrow(() => core.Run("x", parameters),
            "Public runtime execution should tolerate null external values without crashing during input normalization/factory stage.");
    }

    private static BasicCoreImpl<string> CreateCore()
    {
        return new BasicCoreImpl<string>(
            () => new PassThroughLexer(),
            () => new PassThroughParser(),
            () => new PassThroughAstTranslator(),
            () => new PassThroughMethodsTranslator(),
            () => new PassThroughCompiler(),
            () => new PassThroughExecutor(),
            [],
            [],
            []);
    }

    private sealed class PassThroughLexer : ILexer
    {
        public LexerConfiguration Configuration { get; } = new([]);

        public List<LexemeValue> Lexemize(string code) => [new(code, null, -1, null)];
    }

    private sealed class PassThroughParser : IParser
    {
        public ParserConfiguration Configuration { get; } = new(new LevelCollection<float, IAstNodeCreator>());

        public AstNode Parse(List<LexemeValue> lexemes) => new(ExtensibleEnum<AstNodeTag>.CreateOrGet("Root"), null, []);

        public void ParseScope(AstNode scope, List<IAstNodeCreator> creators, Predicate<AstNode> needToVisit)
        {
        }
    }

    private sealed class PassThroughAstTranslator : IAstToBytecodeTranslator
    {
        public BytecodeTranslatorConfiguration Configuration { get; } = new([]);

        public Bytecode Translate(AstNode root) => new([]);
    }

    private sealed class PassThroughMethodsTranslator : IAbstractMethodsTranslator
    {
        public IAbstractIR Translate(Bytecode bytecode) => new AbstractIR();
    }

    private sealed class PassThroughCompiler : IAbstractIrCompiler<string>
    {
        public string Compile(IAbstractIR air, CompilationInput input) => input.SourceText;
    }

    private sealed class PassThroughExecutor : IExecutor<string>
    {
        public object Execute(string compilation, IExecutionEnvironment environment) => compilation;
    }
}
