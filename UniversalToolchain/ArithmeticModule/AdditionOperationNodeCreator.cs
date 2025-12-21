using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

public class AdditionOperationNodeCreator : BinaryOperationBase
{
    public override ExtensibleEnum<AstNodeTag> AstNodeType => ExtensibleEnum<AstNodeTag>.CreateOrGet("Addition");
}