// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Diagnostics.CodeAnalysis;
using BasicCore.ParserWrapper;
using ExceptionsManager;
using TypeInference.Rules;

namespace TypeInference;

public class ExpressionTypeResolver
{
    private readonly Dictionary<string, Type> _expressionTypeCache = new();
    private readonly List<ITypeInferenceRule> _rules = new();

    public ExpressionTypeResolver()
    {
        RegisterDefaultRules();
    }

    public void RegisterRule(ITypeInferenceRule rule)
    {
        _rules.Add(rule);
        _rules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public Type? ResolveExpressionType(AstNode node, TypeInferenceContext context)
    {
        return !TryResolveExpressionType(node, context, out var type)
            ? null
            : type;
    }

    public bool TryResolveExpressionType(AstNode node, TypeInferenceContext context, [NotNullWhen(true)] out Type? type)
    {
        node.NotNull("Node cannot be null");

        type = null;

        var cacheKey = CreateCacheKey(node, context);
        if (_expressionTypeCache.TryGetValue(cacheKey, out type))
            return true;

        foreach (var rule in _rules)
            if (rule.CanTryToInferType(node, context))
            {
                type = rule.TryInferType(node, context);
                if (type != null)
                {
                    _expressionTypeCache[cacheKey] = type;
                    return true;
                }
            }

        return false;
    }

    private string CreateCacheKey(AstNode node, TypeInferenceContext context)
    {
        var contextVars = string.Join(";",
            context.GetAllVariables().OrderBy(v => v.Key).Select(v => $"{v.Key}:{v.Value.Name}"));
        return $"{node.NodeType}:{node.Text}:{contextVars}";
    }

    private void RegisterDefaultRules()
    {
        RegisterRule(new LiteralTypeInferenceRule());
        RegisterRule(new VariableTypeInferenceRule());
        RegisterRule(new BinaryOperationTypeInferenceRule());
        RegisterRule(new FunctionCallTypeInferenceRule());
        RegisterRule(new ScopeTypeInferenceRule());
        RegisterRule(new UnaryOperationTypeInferenceRule());
    }
}