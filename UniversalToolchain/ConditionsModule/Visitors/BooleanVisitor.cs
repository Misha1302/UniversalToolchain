namespace ConditionsModule.Visitors;

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

        // Generate unique labels for short-circuit flow control.
        var falseLabel = Guid.NewGuid();
        var trueLabel = Guid.NewGuid();
        var endLabel = Guid.NewGuid();

        // 1. Evaluate the left operand.
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);

        // 2. Branch based on the operation kind.
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


        // 3. Evaluate the right operand.
        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);

        // 4. For AND: both true => true, otherwise false.
        //    For OR: both false => false, otherwise true.
        if (isAnd)
        {
            // After evaluating the right operand for AND, false jumps to the false label.
            var rightFalseJump = new AbstractMethodImpl(
                $"BoolAndRightFalse_{falseLabel}",
                (il, _) => il.JmpIfNot(falseLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(rightFalseJump));

            // Both are true, branch to the true label.
            var jumpToTrue = new AbstractMethodImpl(
                $"BoolAndJumpTrue_{trueLabel}",
                (il, _) => il.Jmp(trueLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(jumpToTrue));
        }
        else // OR
        {
            // After evaluating the right operand for OR, true jumps to the true label.
            var rightTrueJump = new AbstractMethodImpl(
                $"BoolOrRightTrue_{trueLabel}",
                (il, _) => il.JmpIf(trueLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(rightTrueJump));

            // Both are false, branch to the false label.
            var jumpToFalse = new AbstractMethodImpl(
                $"BoolOrJumpFalse_{falseLabel}",
                (il, _) => il.Jmp(falseLabel)
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(jumpToFalse));
        }

        // 5. False label emits false.
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

        // 6. True label emits true.
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

        // 7. End label joins control flow.
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