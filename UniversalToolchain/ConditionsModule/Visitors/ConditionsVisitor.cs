using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;

namespace ConditionsModule.Visitors;

public class ConditionsVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;
        if (nodeType != ExtensibleEnum<AstNodeTag>.Get("If") &&
            nodeType != ExtensibleEnum<AstNodeTag>.Get("Elif") &&
            nodeType != ExtensibleEnum<AstNodeTag>.Get("Else"))
            return;

        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("If") || nodeType == ExtensibleEnum<AstNodeTag>.Get("Elif"))
            VisitIf(data);
        else if (nodeType == ExtensibleEnum<AstNodeTag>.Get("Else"))
            VisitElse(data);
    }

    private void VisitIf(BytecodeVisitorData data)
    {
        // If condition
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);

        var endLabel = Guid.NewGuid();
        var elseLabel = Guid.NewGuid();

        // Conditional jump if false
        var condJumpMethod = new AbstractMethodImpl(
            $"CondFGoto_!Intrinsic_{elseLabel}",
            (il, _) => il.JmpIfNot(elseLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(condJumpMethod));

        // If body
        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);

        // Unconditional jump to end
        var jumpMethod = new AbstractMethodImpl(
            $"Goto_!Intrinsic_{endLabel}",
            (il, _) => il.Jmp(endLabel)
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(jumpMethod));

        // Else label
        var elseLabelMethod = new AbstractMethodImpl(
            $"Label_!Intrinsic_{elseLabel}",
            (il, _) => il.SetLabel(elseLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(elseLabelMethod));

        // Process elif/else if present
        if (data.Node.Children.Count > 2)
            for (var i = 2; i < data.Node.Children.Count; i++)
                data.AstToBytecodeTranslator.Translate(data.Node.Children[i]);

        // End label
        var endLabelMethod = new AbstractMethodImpl(
            $"Label_!Intrinsic_{endLabel}",
            (il, _) => il.SetLabel(endLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(endLabelMethod));
    }

    private void VisitElse(BytecodeVisitorData data)
    {
        // Simply execute else body
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
    }
}