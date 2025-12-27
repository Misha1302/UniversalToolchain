using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;

namespace ConditionsModule;

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
        // Условие if
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);

        var endLabel = Guid.NewGuid();
        var elseLabel = Guid.NewGuid();

        // Условный переход если false
        var condJumpMethod = new AbstractMethodImpl(
            $"CondFGoto_!Intrinsic_{elseLabel}",
            (il, _) => il.JmpIfNot(elseLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(condJumpMethod));

        // Тело if
        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);

        // Безусловный переход в конец
        var jumpMethod = new AbstractMethodImpl(
            $"Goto_!Intrinsic_{endLabel}",
            (il, _) => il.Jmp(endLabel)
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(jumpMethod));

        // Метка else
        var elseLabelMethod = new AbstractMethodImpl(
            $"Label_!Intrinsic_{elseLabel}",
            (il, _) => il.SetLabel(elseLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(elseLabelMethod));

        // Обработка elif/else если есть
        if (data.Node.Children.Count > 2)
            for (var i = 2; i < data.Node.Children.Count; i++)
                data.AstToBytecodeTranslator.Translate(data.Node.Children[i]);

        // Метка конца
        var endLabelMethod = new AbstractMethodImpl(
            $"Label_!Intrinsic_{endLabel}",
            (il, _) => il.SetLabel(endLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(endLabelMethod));
    }

    private void VisitElse(BytecodeVisitorData data)
    {
        // Просто выполняем тело else
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
    }
}