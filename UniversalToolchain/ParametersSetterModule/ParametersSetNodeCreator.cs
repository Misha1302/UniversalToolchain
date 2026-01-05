using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ParametersSetterModule;

public class ParametersSetNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType => ExtensibleEnum<AstNodeTag>.CreateOrGet("DefineParameter");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.LexemeType != ExtensibleEnum<LexemeTag>.CreateOrGet("Preprocessor lexeme"))
            return false;
        scope[childIndex].NodeType = AstNodeType;
        return true;
    }
}