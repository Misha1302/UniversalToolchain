using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using UniversalIntermediateRepresentation;

namespace ArithmeticModule;

public class ArithmeticAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (ArithmeticModuleImpl.Ops.All(op => data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet(op)))
            return;

        foreach (var child in data.Node.Children)
            data.BytecodeTranslator.Translate(child);

        var op = (data.Node.LexemeValue?.Text).NotNull();

        var method = new AbstractMethodImpl(
            $"Op_{op}",
            2,
            (il, context) =>
            {
                // arguments already pushed
                il.CallCSharp(
                    context.Stack[^1].GetMethod(op switch
                        {
                            "+" => "Add",
                            "-" => "Sub",
                            "*" => "Mul",
                            "/" => "Div",
                            _ => Thrower.InvalidOpEx<string>()
                        }
                    ).NotNull()
                );
            },
            context => context.Stack[0]
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}