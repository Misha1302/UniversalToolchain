// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;

namespace BasicCodeTranslator;

public class BasicBytecodeBytecodeTranslatorImpl(BytecodeTranslatorConfiguration configuration) : IBytecodeTranslator
{
    private readonly Bytecode _code = new([]);

    public BasicBytecodeBytecodeTranslatorImpl() : this(new BytecodeTranslatorConfiguration([]))
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