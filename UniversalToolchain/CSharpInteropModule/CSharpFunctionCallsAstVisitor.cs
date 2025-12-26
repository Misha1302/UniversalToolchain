using AbstractIrExtensions;
using AssemblyFinder;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace CSharpInteropModule;

public class CSharpFunctionCallsAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("CSharpFunctionCall"))
            return;

        foreach (var child in data.Node.Children)
            data.BytecodeTranslator.Translate(child);

        var fullName = (data.Node.LexemeValue?.Text).NotNull();
        var call = MethodsFinder.GetMethod(fullName).NotNull();
        var argsCount = data.Node.Children[0].Children.Count;

        var method = new AbstractMethodImpl(
            $"Call_{fullName}",
            argsCount,
            (il, _) => il.CallCSharp(call),
            context => !call.ReturnType.IsGenericParameter ? call.ReturnType : context.Stack[0]
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}