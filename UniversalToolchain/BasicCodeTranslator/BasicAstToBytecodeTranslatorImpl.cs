using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;

namespace BasicCodeTranslator;

public class BasicAstToBytecodeTranslatorImpl(BytecodeTranslatorConfiguration configuration) : IAstToBytecodeTranslator
{
    private readonly Bytecode _code = new([]);
    private int _translationDepth;

    public BasicAstToBytecodeTranslatorImpl() : this(new BytecodeTranslatorConfiguration([]))
    {
    }

    public BytecodeTranslatorConfiguration Configuration { get; } = configuration;

    public Bytecode Translate(AstNode root)
    {
        if (_translationDepth == 0)
            _code.Instructions.Clear();

        _translationDepth++;
        try
        {
            var data = new BytecodeVisitorData(this, _code, root);
            foreach (var visitor in Configuration.Visitors)
                visitor.TryVisit(data);

            return _code;
        }
        finally
        {
            _translationDepth--;
        }
    }
}
