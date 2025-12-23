using System.Reflection;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using UniversalIntermediateRepresentation;

namespace EqualityModule;

public class EqualityAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality"))
            return;

        data.BytecodeTranslator.Translate(data.Node.Children[1]); // value
        data.BytecodeTranslator.Translate(data.Node.Children[0]); // ref

        var method = new AbstractMethodImpl(
            $"Set_{data.Node.Children[0].LexemeValue?.Text}={data.Node.Children[1].LexemeValue?.Text}",
            2,
            (il, context) =>
            {
                il.Rotate(2);
                il.CallCSharp(context.Stack[^1].GetMethod("SetValue", BindingFlags.Instance | BindingFlags.Public).NotNull());
            },
            _ => typeof(void)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}