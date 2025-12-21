using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

public class DivisionOperationNodeCreator : BinaryOperationBase
{
    public override ExtensibleEnum<AstNodeTag> AstNodeType => ExtensibleEnum<AstNodeTag>.CreateOrGet("Division");
}