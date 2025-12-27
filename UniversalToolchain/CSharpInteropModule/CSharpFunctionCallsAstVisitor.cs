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
            data.AstToBytecodeTranslator.Translate(child);

        var fullName = (data.Node.LexemeValue?.Text).NotNull();
        var call = MethodsFinder.GetMethod(fullName).NotNull();

        var method = new AbstractMethodImpl(
            $"Call_{fullName}",
            (il, _) => il.CallCSharp(call)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}