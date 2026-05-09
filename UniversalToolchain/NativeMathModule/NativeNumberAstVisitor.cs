namespace NativeMathModule;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class NativeNumberAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;

        if (nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeNumber"))
            return;

        var lexeme = data.Node.LexemeValue;
        Thrower.AssertAlways(lexeme != null, "NativeNumber node must contain lexeme value.");
        var numText = lexeme.Text;
        var value = NativeTypesModuleImpl.ParseNumber(numText);
        var valueType = value.GetType();

        var method = new AbstractMethodImpl(
            $"PushNative_{valueType.Name}_{value}",
            (il, _) =>
            {
                // Push the parsed value to the stack.
                il.Push(value);

                // Numeric types need no extra handling
                // (unlike RealNumberImpl, which requires a constructor call).
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}