// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using IlCodeGeneratorFactory;

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

        var method = new DynamicMethodConvertableWrapperImpl();
        var op = data.Node.LexemeValue?.Text ?? GetOperatorFromNodeType(nodeType);

        method.Make($"Comparison_{op}", typeof(bool), [null, null], (il, args) =>
        {
            il.LdArgsAndCall(
                typeof(Comparisons).GetMethod(GetComparisonMethodName(op)).NotNull(),
                i =>
                {
                    il.Ldarg(i);
                    return args[i];
                });
            il.Ret();
        });

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
            _ => "=="
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
            _ => "Equal"
        };
    }
}