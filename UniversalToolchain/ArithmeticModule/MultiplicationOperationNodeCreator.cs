using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

public class MultiplicationOperationNodeCreator : BinaryOperationBase
{
    public override ExtensibleEnum<AstNodeTag> AstNodeType => ExtensibleEnum<AstNodeTag>.CreateOrGet("Multiplication");
}