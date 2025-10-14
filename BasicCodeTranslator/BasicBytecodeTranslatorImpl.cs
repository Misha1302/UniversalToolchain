// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicParser;

namespace BasicCodeTranslator;

public class BasicBytecodeTranslatorImpl(BytecodeTranslatorConfiguration configuration) : IBytecodeTranslator
{
    private readonly Bytecode _code = new([]);

    public Bytecode Translate(AstNode root)
    {
        var data = new VisitorData(this, _code, root);
        foreach (var visitor in configuration.Visitors)
            visitor.TryVisit(data);
        return _code;
    }
}