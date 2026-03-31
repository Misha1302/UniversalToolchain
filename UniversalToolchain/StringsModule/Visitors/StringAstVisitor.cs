namespace StringsModule.Visitors;

[AutoRegisterService]
public class StringAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("String"))
            return;

        var literalText = (data.Node.LexemeValue?.Text).NotNull();
        var value = StringLiteralDecoder.Decode(literalText);

        var method = new AbstractMethodImpl(
            $"PushString_{value}",
            (il, _) =>
            {
                il.Push(value);
                il.CallCSharp(typeof(WistStringImpl).GetMethod(nameof(WistStringImpl.Create), [typeof(string)]).NotNull());
            });

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}
