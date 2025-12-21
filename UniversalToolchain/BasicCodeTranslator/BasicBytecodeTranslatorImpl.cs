using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;

namespace BasicCodeTranslator;

public class BasicBytecodeTranslatorImpl(BytecodeTranslatorConfiguration configuration) : IBytecodeTranslator
{
    private readonly Bytecode _code = new([]);

    public BasicBytecodeTranslatorImpl() : this(new BytecodeTranslatorConfiguration([]))
    {
    }

    public BytecodeTranslatorConfiguration Configuration { get; } = configuration;

    public Bytecode Translate(AstNode root)
    {
        var data = new BytecodeVisitorData(this, _code, root);
        foreach (var visitor in Configuration.Visitors)
            visitor.TryVisit(data);
        return _code;
    }
}