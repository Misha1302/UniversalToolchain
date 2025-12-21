using BasicCore.TranslatorWrapper;

namespace BasicCore.ParserWrapper;

public interface IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data);
}