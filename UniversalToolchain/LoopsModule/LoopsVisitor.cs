using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;

namespace LoopsModule;

public class LoopsVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;
        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("While"))
            VisitWhile(data);
        else if (nodeType == ExtensibleEnum<AstNodeTag>.Get("For"))
            VisitFor(data);
    }

    private static void VisitWhile(BytecodeVisitorData data)
    {
        var loopStartLabel = Guid.NewGuid();
        var loopEndLabel = Guid.NewGuid();

        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl($"WhileStart_!Intrinsic_{loopStartLabel}", (il, _) => il.SetLabel(loopStartLabel))));

        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl($"WhileExit_!Intrinsic_{loopEndLabel}", (il, _) => il.JmpIfNot(loopEndLabel))));

        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);

        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl($"WhileBack_!Intrinsic_{loopStartLabel}", (il, _) => il.Jmp(loopStartLabel))));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl($"WhileEnd_!Intrinsic_{loopEndLabel}", (il, _) => il.SetLabel(loopEndLabel))));
    }

    private static void VisitFor(BytecodeVisitorData data)
    {
        var loopStartLabel = Guid.NewGuid();
        var loopEndLabel = Guid.NewGuid();

        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]); // init

        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl($"ForStart_!Intrinsic_{loopStartLabel}", (il, _) => il.SetLabel(loopStartLabel))));

        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]); // condition

        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl($"ForExit_!Intrinsic_{loopEndLabel}", (il, _) => il.JmpIfNot(loopEndLabel))));

        data.AstToBytecodeTranslator.Translate(data.Node.Children[3]); // body
        data.AstToBytecodeTranslator.Translate(data.Node.Children[2]); // step

        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl($"ForBack_!Intrinsic_{loopStartLabel}", (il, _) => il.Jmp(loopStartLabel))));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl($"ForEnd_!Intrinsic_{loopEndLabel}", (il, _) => il.SetLabel(loopEndLabel))));
    }
}
