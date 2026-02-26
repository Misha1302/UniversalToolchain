using System.Reflection;
using AbstractIrExtensions;
using BasicCore.Attributes;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace NativeMathModule;

[AutoRegisterService]
public class NativeArithmeticAstVisitor : IAstVisitor
{
    private static readonly Dictionary<string, string> _opToMethodName = new()
    {
        ["+"] = "Add",
        ["-"] = "Subtract",
        ["*"] = "Multiply",
        ["/"] = "Divide"
    };

    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;

        if (nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeAddition") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeSubtraction") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeMultiplication") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeDivision"))
            return;

        // Обрабатываем оба операнда
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);

        var opSymbol = data.Node.LexemeValue.Text;
        var methodName = _opToMethodName[opSymbol];

        var method = new AbstractMethodImpl(
            $"NativeArithmetic_{methodName}",
            (il, context) =>
            {
                // Используем обобщенные методы из INumber<T>
                var leftType = context.Stack[^2];
                var rightType = context.Stack[^1];
                Thrower.AssertAlways(leftType == rightType);

                if (leftType == typeof(decimal))
                {
                    var decimalMethod = typeof(NativeArithmetic)
                        .GetMethod(methodName + "Decimal", BindingFlags.Static | BindingFlags.Public)
                        .NotNull();

                    il.CallCSharp(decimalMethod);
                }
                else
                {
                    var genericMethod = typeof(NativeArithmetic)
                        .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)
                        .NotNull()
                        .MakeGenericMethod(leftType);

                    il.CallCSharp(genericMethod);
                }
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}