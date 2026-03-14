
namespace BasicCodeTranslator;

public class BasicAstToBytecodeTranslatorImpl(BytecodeTranslatorConfiguration configuration) : IAstToBytecodeTranslator
{
    public BasicAstToBytecodeTranslatorImpl() : this(new BytecodeTranslatorConfiguration([]))
    {
    }

    public BytecodeTranslatorConfiguration Configuration { get; } = configuration;

    public Bytecode Translate(AstNode root)
    {
        var bytecode = new Bytecode([]);
        var requestTranslator = new RequestTranslator(Configuration, bytecode);

        requestTranslator.Translate(root);

        return bytecode;
    }

    private sealed class RequestTranslator(BytecodeTranslatorConfiguration configuration, Bytecode bytecode)
        : IAstToBytecodeTranslator
    {
        public BytecodeTranslatorConfiguration Configuration { get; } = configuration;

        public Bytecode Translate(AstNode root)
        {
            var data = new BytecodeVisitorData(this, bytecode, root);
            foreach (var visitor in Configuration.Visitors)
                visitor.TryVisit(data);

            return bytecode;
        }
    }
}