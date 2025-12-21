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
        data.BytecodeTranslator.Translate(data.Node.Children[0]);

        var endLabel = Guid.NewGuid().ToString("N");
        var elseLabel = Guid.NewGuid().ToString("N");

        // Условный переход если false
        var condJumpMethod = new DynamicMethodConvertableWrapperImpl();
        condJumpMethod.Make($"CondFGoto_!Intrinsic_{elseLabel}", 1,
            (il, _) =>
            {
                il.Ldarg(0);
                il.Brfalse(il.DefineLabel(elseLabel));
                il.Ret();
            }, _ => typeof(void));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(condJumpMethod));

        // Тело if
        data.BytecodeTranslator.Translate(data.Node.Children[1]);

        // Безусловный переход в конец
        var jumpMethod = new DynamicMethodConvertableWrapperImpl();
        jumpMethod.Make($"Goto_!Intrinsic_{endLabel}", 0,
            (il, _) =>
            {
                il.Br(il.DefineLabel(endLabel));
                il.Ret();
            }, _ => typeof(void));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(jumpMethod));

        // Метка else
        var elseLabelMethod = new DynamicMethodConvertableWrapperImpl();
        elseLabelMethod.Make($"Label_!Intrinsic_{elseLabel}", 0,
            (il, _) =>
            {
                il.MarkLabel(il.DefineLabel(elseLabel));
                il.Ret();
            }, _ => typeof(void));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(elseLabelMethod));

        // Обработка elif/else если есть
        if (data.Node.Children.Count > 2)
            for (var i = 2; i < data.Node.Children.Count; i++)
                data.BytecodeTranslator.Translate(data.Node.Children[i]);

        // Метка конца
        var endLabelMethod = new DynamicMethodConvertableWrapperImpl();
        endLabelMethod.Make($"Label_!Intrinsic_{endLabel}", 0,
            (il, _) =>
            {
                il.MarkLabel(il.DefineLabel(endLabel));
                il.Ret();
            }, _ => typeof(void));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(endLabelMethod));
    }

    private void VisitElse(BytecodeVisitorData data)
    {
        // Просто выполняем тело else
        data.BytecodeTranslator.Translate(data.Node.Children[0]);
    }
}