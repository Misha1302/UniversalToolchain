using AbstractIrExtensions;
using BasicCore;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using JetBrains.Annotations;

namespace ConditionsModule;

[AutoRegisterService]
public class BooleanVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;
        if (nodeType == ExtensibleEnum<AstNodeTag>.Get("True") ||
            nodeType == ExtensibleEnum<AstNodeTag>.Get("False"))
            VisitBooleanLiteral(data);
        else if (nodeType == ExtensibleEnum<AstNodeTag>.Get("And") ||
                 nodeType == ExtensibleEnum<AstNodeTag>.Get("Or"))
            VisitBooleanOperationWithShortCircuit(data);
        else if (nodeType == ExtensibleEnum<AstNodeTag>.Get("Not"))
            VisitBooleanOperation(data);
    }

    private void VisitBooleanLiteral(BytecodeVisitorData data)
    {
        var value = data.Node.NodeType == ExtensibleEnum<AstNodeTag>.Get("True");
        var method = new AbstractMethodImpl(
            $"PushBoolean_{value}",
            (il, _) => il.Push(value)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void VisitBooleanOperation(BytecodeVisitorData data)
    {
        foreach (var child in data.Node.Children)
            data.AstToBytecodeTranslator.Translate(child);

        var op = data.Node.NodeType;
        var method = new AbstractMethodImpl(
            $"Boolean_{op}",
            (il, context) =>
            {
                if (context.Stack[^1] != typeof(bool))
                    il.CallCSharp(context.Stack[^1].GetMethod(op.GetName()).NotNull());
                else
                    il.CallCSharp(typeof(BooleanOperations).GetMethod(op.GetName()).NotNull());
            }
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private void VisitBooleanOperationWithShortCircuit(BytecodeVisitorData data)
    {
        var op = data.Node.NodeType;
        var isAnd = op.GetName() == "And";

        // Генерируем уникальные метки для управления потоком
        var falseLabel = Guid.NewGuid();
        var trueLabel = Guid.NewGuid();
        var endLabel = Guid.NewGuid();

        // 1. Вычисляем левый операнд
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);

        // 2. Условный переход в зависимости от операции
        if (isAnd)
        {
            var condJumpMethod = new AbstractMethodImpl(
                $"BoolCondJump_{op.GetName()}_{falseLabel}",
                (il, _) => il.JmpIfNot(falseLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(condJumpMethod));
        }
        else
        {
            var condJumpMethod = new AbstractMethodImpl(
                $"BoolCondJump_{op.GetName()}_{trueLabel}",
                (il, _) => il.JmpIf(trueLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(condJumpMethod));
        }


        // 3. Вычисляем правый операнд
        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);

        // 4. Для AND: если оба true -> true, иначе -> false
        //    Для OR: если оба false -> false, иначе -> true
        if (isAnd)
        {
            // После вычисления правого операнда для AND
            // Если правый false -> переход к false
            var rightFalseJump = new AbstractMethodImpl(
                $"BoolAndRightFalse_{falseLabel}",
                (il, _) => il.JmpIfNot(falseLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(rightFalseJump));

            // Оба true - переход к true
            var jumpToTrue = new AbstractMethodImpl(
                $"BoolAndJumpTrue_{trueLabel}",
                (il, _) => il.Jmp(trueLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(jumpToTrue));
        }
        else // OR
        {
            // После вычисления правого операнда для OR
            // Если правый true -> переход к true
            var rightTrueJump = new AbstractMethodImpl(
                $"BoolOrRightTrue_{trueLabel}",
                (il, _) => il.JmpIf(trueLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(rightTrueJump));

            // Оба false - переход к false
            var jumpToFalse = new AbstractMethodImpl(
                $"BoolOrJumpFalse_{falseLabel}",
                (il, _) => il.Jmp(falseLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(jumpToFalse));
        }

        // 5. Метка false (результат false)
        var falseLabelMethod = new AbstractMethodImpl(
            $"BoolFalseLabel_{falseLabel}",
            (il, _) => il.SetLabel(falseLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(falseLabelMethod));

        var pushFalseMethod = new AbstractMethodImpl(
            $"PushBoolean_false_{falseLabel}",
            (il, _) => il.Push(false)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(pushFalseMethod));

        var jumpToEnd = new AbstractMethodImpl(
            $"BoolJumpEnd_{endLabel}",
            (il, _) => il.Jmp(endLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(jumpToEnd));

        // 6. Метка true (результат true)
        var trueLabelMethod = new AbstractMethodImpl(
            $"BoolTrueLabel_{trueLabel}",
            (il, _) => il.SetLabel(trueLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(trueLabelMethod));

        var pushTrueMethod = new AbstractMethodImpl(
            $"PushBoolean_true_{trueLabel}",
            (il, _) => il.Push(true)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(pushTrueMethod));

        // 7. Метка конца
        var endLabelMethod = new AbstractMethodImpl(
            $"BoolEndLabel_{endLabel}",
            (il, _) => il.SetLabel(endLabel)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(endLabelMethod));
    }

    [UsedImplicitly]
    public static class BooleanOperations
    {
        [UsedImplicitly]
        public static bool And(bool a, bool b) => a && b;

        [UsedImplicitly]
        public static bool Or(bool a, bool b) => a || b;

        [UsedImplicitly]
        public static bool Not(bool a) => !a;
    }
}