using AbstractIrExtensions;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;

namespace EqualityModule;

public class EqualityAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality"))
            return;

        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]); // value
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]); // ref

        var method = new AbstractMethodImpl(
            $"Set_{data.Node.Children[0].LexemeValue?.Text}={data.Node.Children[1].LexemeValue?.Text}",
            (il, context) => il.SetValueToSettable(context.Stack[^2])
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}