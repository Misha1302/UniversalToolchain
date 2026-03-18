using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

internal static class DialectDirectiveValidationKeys
{
    public static DialectSetStateKey<string> UseModules { get; } = new(UseModulesDialectDirectiveFeature.FeatureId, StringComparer.Ordinal);
    public static DialectSetStateKey<string> ExcludeModules { get; } = new(ExcludeModulesDialectDirectiveFeature.FeatureId, StringComparer.Ordinal);
    public static DialectValueStateKey<ToggleValidationState> IntrinsicToggle { get; } = new("builtin.intrinsics.toggle");
    public static DialectValueStateKey<ToggleValidationState> OptimizerToggle { get; } = new("builtin.optimizers.toggle");
}

internal abstract class DialectDirectiveFeatureBase : IDialectDirectiveFeature
{
    public abstract string Id { get; }

    public abstract string Keyword { get; }

    public string LexemeTag => $"DialectDirectiveKeyword.{Keyword}";

    public abstract DialectDirectiveParserOrder ParserOrder { get; }

    public virtual bool IsSingleton => false;

    public virtual string SingletonViolationMessage => $"Directive '{Keyword}' can only be declared once.";

    public abstract DialectDirectiveAstNode ParseDirective(AstNode lineNode);

    public virtual void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        Thrower.InvalidOpEx($"Dialect feature '{GetType().Name}' does not support line accumulation.");
    }

    public virtual void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
    }

    public abstract IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive);

    protected static IReadOnlyList<string> GetIdentifierListArgument(DialectDirectiveAstNode directive)
    {
        if (directive.Payload is not IdentifierListAstNode)
        {
            Thrower.Argument(nameof(directive), $"Directive '{directive.Feature.Keyword}' must provide an identifier list payload.");
        }

        var identifierList = (IdentifierListAstNode)directive.Payload;
        if (identifierList.Identifiers.Count == 0)
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directive.Feature.Keyword}' must contain at least one identifier.", directive.LexemeValue);
        }

        foreach (var identifier in identifierList.Identifiers)
        {
            ValidateIdentifier(identifier, $"Directive '{directive.Feature.Keyword}' contains an empty identifier.");
        }

        ValidateNoDuplicates(identifierList.Identifiers.Select(x => x.Identifier), $"Directive '{directive.Feature.Keyword}' contains duplicate identifiers.", directive.LexemeValue);
        return identifierList.Identifiers.Select(x => x.Identifier).ToList();
    }

    protected static string GetSingleIdentifierArgument(DialectDirectiveAstNode directive)
    {
        if (directive.Payload is not IdentifierValueAstNode)
        {
            Thrower.Argument(nameof(directive), $"Directive '{directive.Feature.Keyword}' must provide a single identifier payload.");
        }

        var identifier = (IdentifierValueAstNode)directive.Payload;
        ValidateIdentifier(identifier, $"Directive '{directive.Feature.Keyword}' must not be empty.");
        return identifier.Identifier;
    }

    protected static void ValidateIdentifier(IdentifierValueAstNode identifier, string message)
    {
        if (identifier == null)
        {
            Thrower.ArgumentNull(nameof(identifier));
        }

        if (string.IsNullOrWhiteSpace(identifier.Identifier))
        {
            DialectDefinitionSliceParseErrors.Fail(message, identifier.LexemeValue);
        }
    }

    protected static void ValidateNoDuplicates(IEnumerable<string> values, string message, LexemeValue? token)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!set.Add(value))
            {
                DialectDefinitionSliceParseErrors.Fail(message, token);
            }
        }
    }

    protected static DialectDirectiveAstNode CreateDirectiveNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, AstNode payload)
    {
        return new DialectDirectiveAstNode(feature, lexemeValue, [payload]);
    }
}

internal abstract class IdentifierListDialectDirectiveFeatureBase : DialectDirectiveFeatureBase
{
    public sealed override DialectDirectiveAstNode ParseDirective(AstNode lineNode)
    {
        var identifiers = DialectNodeCreatorSupport.ParseIdentifierList(lineNode, Keyword);
        return CreateDirectiveNode(this, lineNode.Children[0].LexemeValue, identifiers);
    }

    public sealed override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        AccumulateIdentifiers(accumulation, DialectDirectiveParserSupport.ParseIdentifierList(line, Keyword));
    }

    protected abstract void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values);
}

internal abstract class SingleIdentifierDialectDirectiveFeatureBase : DialectDirectiveFeatureBase
{
    public sealed override DialectDirectiveAstNode ParseDirective(AstNode lineNode)
    {
        var identifier = DialectNodeCreatorSupport.ParseSingleIdentifier(lineNode, Keyword);
        return CreateDirectiveNode(this, lineNode.Children[0].LexemeValue, identifier);
    }

    public sealed override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        AccumulateIdentifier(accumulation, DialectDirectiveParserSupport.ParseSingleIdentifier(line, Keyword));
    }

    protected abstract void AccumulateIdentifier(DialectDirectiveAccumulation accumulation, string value);
}

internal sealed class ToggleValidationState(IEqualityComparer<string> comparer)
{
    private readonly HashSet<string> _enabled = new(comparer);
    private readonly HashSet<string> _disabled = new(comparer);

    public void Add(string value, bool enabled, string duplicateMessage, string contradictionMessageTemplate, LexemeValue? token)
    {
        var current = enabled ? _enabled : _disabled;
        var opposite = enabled ? _disabled : _enabled;

        if (!current.Add(value))
        {
            DialectDefinitionSliceParseErrors.Fail(duplicateMessage, token);
        }

        if (opposite.Contains(value))
        {
            DialectDefinitionSliceParseErrors.Fail(string.Format(contradictionMessageTemplate, value), token);
        }
    }
}

internal static class DialectDirectiveParserSupport
{
    public static string ParseSingleIdentifier(IReadOnlyList<LexemeValue> line, string directiveName)
    {
        if (line.Count != 2 || !DialectLexemeTags.IsTag(line[1], DialectLexemeTags.Identifier))
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' expects exactly one identifier argument.", line.ElementAtOrDefault(1) ?? line[0]);
        }

        return line[1].Text;
    }

    public static IReadOnlyList<string> ParseIdentifierList(IReadOnlyList<LexemeValue> line, string directiveName)
    {
        if (line.Count < 2)
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' expects at least one identifier.", line[0]);
        }

        var values = new List<string>();
        var expectIdentifier = true;
        for (var i = 1; i < line.Count; i++)
        {
            var token = line[i];
            if (expectIdentifier)
            {
                if (!DialectLexemeTags.IsTag(token, DialectLexemeTags.Identifier))
                {
                    DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' contains an invalid identifier list item.", token);
                }

                values.Add(token.Text);
                expectIdentifier = false;
                continue;
            }

            if (!DialectLexemeTags.IsTag(token, DialectLexemeTags.CommaToken))
            {
                DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' expects comma-separated identifiers.", token);
            }

            expectIdentifier = true;
        }

        if (expectIdentifier)
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' must not end with a trailing comma.", line[^1]);
        }

        return values;
    }
}
