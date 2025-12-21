using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

public class SubstractionOperationNodeCreator : BinaryOperationBase
{
    public override ExtensibleEnum<AstNodeTag> AstNodeType => ExtensibleEnum<AstNodeTag>.CreateOrGet("Substraction");
}