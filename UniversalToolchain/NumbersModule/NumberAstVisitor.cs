using System.Globalization;
using AbstractIrExtensions;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace NumbersModule;

public class NumberAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Number")) return;

        var numText = (data.Node.LexemeValue?.Text).NotNull();
        var num = double.Parse(numText, NumberStyles.Any);

        var method = new AbstractMethodImpl(
            $"PushNumber_{num}",
            0,
            (il, _) =>
            {
                il.Push(num);
                il.CallCSharp(typeof(RealNumberImpl).GetConstructor([typeof(double)]).NotNull());
            }, _ => typeof(RealNumberImpl));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}