namespace CSharpInteropModule.Visitors;

public class CSharpFunctionCallsAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("CSharpFunctionCall"))
            return;

        var argsScope = data.Node.Children[0];
        var arguments = argsScope.Children;

        // Translate arguments; they will be pushed onto the stack.
        foreach (var argument in arguments)
            data.AstToBytecodeTranslator.Translate(argument);

        var fullName = (data.Node.LexemeValue?.Text).NotNull();

        var method = new AbstractMethodImpl(
            $"Call_{fullName}",
            (il, context) =>
            {
                // Use stack types for overload resolution.
                // Argument count equals the child count of the args node.
                var argCount = arguments.Count;
                var stackTypes = context.Stack.TakeLast(argCount).ToList();

                // Try to find a method with matching parameter types first.
                var methodInfo = MethodsFinder.GetMethod(fullName, stackTypes.ToArray())
                                 ?? MethodsFinder.GetMethod(fullName, argCount)
                                 ?? MethodsFinder.GetMethod(fullName);

                if (methodInfo == null)
                    WistThrower.Import($"Method '{fullName}({argCount} args)' not found in imported assemblies.");

                il.CallCSharp(methodInfo.NotNull());
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}