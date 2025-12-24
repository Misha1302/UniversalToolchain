using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using UniversalIntermediateRepresentation;

namespace ConditionsModule;

public class ComparisonVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;
        if (nodeType != ExtensibleEnum<AstNodeTag>.Get("Equal") &&
            nodeType != ExtensibleEnum<AstNodeTag>.Get("NotEqual") &&
            nodeType != ExtensibleEnum<AstNodeTag>.Get("Greater") &&
            nodeType != ExtensibleEnum<AstNodeTag>.Get("Less") &&
            nodeType != ExtensibleEnum<AstNodeTag>.Get("GreaterOrEqual") &&
            nodeType != ExtensibleEnum<AstNodeTag>.Get("LessOrEqual"))
            return;

        // Сначала вычисляем оба операнда
        data.BytecodeTranslator.Translate(data.Node.Children[0]);
        data.BytecodeTranslator.Translate(data.Node.Children[1]);

        var op = data.Node.LexemeValue?.Text ?? GetOperatorFromNodeType(nodeType);

        var method = new AbstractMethodImpl(
            $"Comparison_{op}",
            2,
            (il, _) =>
            {
                // args already pushed
                il.CallCSharp(
                    typeof(Comparisons).GetMethod(GetComparisonMethodName(op)).NotNull()
                );
            }, _ => typeof(bool));

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private string GetOperatorFromNodeType(ExtensibleEnum<AstNodeTag> nodeType)
    {
        return nodeType.GetName() switch
        {
            "Equal" => "==",
            "NotEqual" => "!=",
            "Greater" => ">",
            "Less" => "<",
            "GreaterOrEqual" => ">=",
            "LessOrEqual" => "<=",
            _ => Thrower.InvalidOpEx<string>()
        };
    }

    private string GetComparisonMethodName(string op)
    {
        return op switch
        {
            "==" => "Equal",
            "!=" => "NotEqual",
            ">" => "Greater",
            "<" => "Less",
            ">=" => "GreaterOrEqual",
            "<=" => "LessOrEqual",
            _ => Thrower.InvalidOpEx<string>()
        };
    }
}