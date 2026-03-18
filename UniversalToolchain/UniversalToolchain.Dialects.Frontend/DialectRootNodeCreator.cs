using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectRootNodeCreator : IAstNodeCreator
{
    public BasicTypesExtensions.ExtensibleEnum<AstNodeTag> AstNodeType => DialectAstNodeTypes.DialectRoot;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.NodeType != DialectAstNodeTypes.Scope || scope.Parent != null || childIndex != 0)
            return false;

        if (scope.Children.Count == 1 && scope.Children[0] is DialectRootAstNode)
            return false;

        var tokens = scope.Children.Where(x => x.LexemeValue != null).Select(x => x.LexemeValue!).ToList();
        if (tokens.Count == 0)
            DialectDefinitionSliceParseErrors.Fail("Dialect source is empty.", null);

        var parser = new DialectDirectiveAstNodeFactory(tokens);
        var root = parser.ParseRoot();

        scope.Children.Clear();
        scope.Children.Add(root);
        return true;
    }
}

public sealed class DialectDirectiveAstNodeFactory(List<LexemeValue> tokens)
{
    private int _position;

    public DialectRootAstNode ParseRoot()
    {
        ExpectTag(DialectLexemeTags.DialectKeyword, "Expected header: dialect <Name>.");
        var name = ReadIdentifier("Expected header: dialect <Name>.");
        RequireDirectiveEnd("Expected newline after dialect header.");

        var directives = new List<DialectDirectiveAstNode>();
        while (!IsEnd())
        {
            SkipNewLines();
            if (IsEnd())
                break;

            directives.Add(ParseDirective());
            RequireDirectiveEnd("Each directive must end with a newline.");
        }

        return new DialectRootAstNode(name, directives);
    }

    private DialectDirectiveAstNode ParseDirective()
    {
        if (TryReadTag(DialectLexemeTags.UseKeyword))
            return new UseModulesDirectiveAstNode([ReadIdentifier("Expected module name after 'use'.")]);

        if (TryReadTag(DialectLexemeTags.ExcludeKeyword))
            return new ExcludeModulesDirectiveAstNode([ReadIdentifier("Expected module name after 'exclude'.")]);

        if (TryReadTag(DialectLexemeTags.RequiresKeyword))
            return ParseOrder("requires", (left, right) => new FrontendOrderDirectiveAstNode([left, right]));

        if (TryReadTag(DialectLexemeTags.BeforeKeyword))
            return ParseOrder("before", (left, right) => new MiddleEndOrderDirectiveAstNode([left, right]));

        if (TryReadTag(DialectLexemeTags.AfterKeyword))
            return ParseOrder("after", (left, right) => new BackendOrderDirectiveAstNode([left, right]));

        if (TryReadTag(DialectLexemeTags.BackendKeyword))
            return ParseBackendDirective();

        if (TryReadTag(DialectLexemeTags.AllowKeyword))
            return ParseIntrinsicDirective(allow: true);

        if (TryReadTag(DialectLexemeTags.ForbidKeyword))
            return ParseIntrinsicDirective(allow: false);

        if (TryReadTag(DialectLexemeTags.EnableKeyword))
            return ParseOptimizerDirective(enabled: true);

        if (TryReadTag(DialectLexemeTags.DisableKeyword))
            return ParseOptimizerDirective(enabled: false);

        if (TryReadTag(DialectLexemeTags.SecurityKeyword))
            return ParseSecurityDirective();

        if (TryReadTag(DialectLexemeTags.CapabilityKeyword))
            return ParseCapabilityDirective();

        DialectDefinitionSliceParseErrors.Fail("Unknown directive in dialect source.", Current());
        return Thrower.InvalidOpEx<DialectDirectiveAstNode>();
    }

    private DialectDirectiveAstNode ParseOrder(string directiveName, Func<string, string, DialectDirectiveAstNode> nodeFactory)
    {
        var left = ReadIdentifier($"Expected order directive format: {directiveName} <Left> -> <Right>.");
        ExpectTag(DialectLexemeTags.ArrowToken, $"Expected order directive format: {directiveName} <Left> -> <Right>.");
        var right = ReadIdentifier($"Expected order directive format: {directiveName} <Left> -> <Right>.");
        return nodeFactory(left, right);
    }

    private AllowedBackendDirectiveAstNode ParseBackendDirective()
    {
        var target = ParseBackendTarget(ReadAny("Expected backend target."), false);
        var enabled = ParseEnableDisable(ReadAny("Expected enable|disable token."));
        return new AllowedBackendDirectiveAstNode(target, enabled);
    }

    private RequiredIntrinsicDirectiveAstNode ParseIntrinsicDirective(bool allow)
    {
        ExpectTag(DialectLexemeTags.IntrinsicKeyword, "Expected intrinsic directive format: allow|forbid intrinsic \"name\" for <interpreter|cil|any>.");
        var intrinsic = ReadStringLiteral("Expected intrinsic directive format: allow|forbid intrinsic \"name\" for <interpreter|cil|any>.");
        ExpectTag(DialectLexemeTags.ForKeyword, "Expected intrinsic directive format: allow|forbid intrinsic \"name\" for <interpreter|cil|any>.");
        var target = ParseBackendTarget(ReadAny("Expected backend target."), true);
        return new RequiredIntrinsicDirectiveAstNode(intrinsic, allow, target);
    }

    private RequiredOptimizerDirectiveAstNode ParseOptimizerDirective(bool enabled)
    {
        ExpectTag(DialectLexemeTags.OptimizerKeyword, "Expected optimizer directive format: enable|disable optimizer <Name> for <interpreter|cil|any>.");
        var optimizer = ReadIdentifier("Expected optimizer name.");
        ExpectTag(DialectLexemeTags.ForKeyword, "Expected optimizer directive format: enable|disable optimizer <Name> for <interpreter|cil|any>.");
        var target = ParseBackendTarget(ReadAny("Expected backend target."), true);
        return new RequiredOptimizerDirectiveAstNode(optimizer, enabled, target);
    }

    private SecurityModeDirectiveAstNode ParseSecurityDirective()
    {
        var token = ReadAny("Expected security mode trusted|restricted.");
        return token.LexemePattern?.LexemeType.GetName() switch
        {
            DialectLexemeTags.TrustedKeyword => new SecurityModeDirectiveAstNode(DialectSecurityProfile.Trusted),
            DialectLexemeTags.RestrictedKeyword => new SecurityModeDirectiveAstNode(DialectSecurityProfile.Restricted),
            _ => ThrowSecurityMode(token)
        };
    }

    private CapabilityDirectiveAstNode ParseCapabilityDirective()
    {
        var capability = ReadIdentifier("Expected capability name.");
        ExpectTag(DialectLexemeTags.EqualsToken, "Expected capability directive format: capability <name> = true|false.");
        var value = ParseBoolean(ReadAny("Expected capability value true|false."));
        return new CapabilityDirectiveAstNode(capability, value);
    }

    private static SecurityModeDirectiveAstNode ThrowSecurityMode(LexemeValue token)
    {
        DialectDefinitionSliceParseErrors.Fail("Security profile must be 'trusted' or 'restricted'.", token);
        return Thrower.InvalidOpEx<SecurityModeDirectiveAstNode>();
    }

    private DialectBackendTarget ParseBackendTarget(LexemeValue token, bool allowAny)
    {
        return token.LexemePattern?.LexemeType.GetName() switch
        {
            DialectLexemeTags.InterpreterKeyword => DialectBackendTarget.Interpreter,
            DialectLexemeTags.CilKeyword => DialectBackendTarget.Cil,
            DialectLexemeTags.AnyKeyword when allowAny => DialectBackendTarget.Any,
            _ => ThrowInvalidBackend(token, allowAny)
        };
    }

    private static bool ParseEnableDisable(LexemeValue token)
    {
        return token.LexemePattern?.LexemeType.GetName() switch
        {
            DialectLexemeTags.EnableKeyword => true,
            DialectLexemeTags.DisableKeyword => false,
            _ => ThrowEnableDisable(token)
        };
    }

    private static bool ThrowEnableDisable(LexemeValue token)
    {
        DialectDefinitionSliceParseErrors.Fail("Expected enable|disable token.", token);
        return false;
    }

    private static bool ParseBoolean(LexemeValue token)
    {
        return token.LexemePattern?.LexemeType.GetName() switch
        {
            DialectLexemeTags.TrueKeyword => true,
            DialectLexemeTags.FalseKeyword => false,
            _ => ThrowBoolean(token)
        };
    }

    private static bool ThrowBoolean(LexemeValue token)
    {
        DialectDefinitionSliceParseErrors.Fail("Capability value must be true or false.", token);
        return false;
    }

    private static DialectBackendTarget ThrowInvalidBackend(LexemeValue token, bool allowAny)
    {
        var expected = allowAny ? "interpreter|cil|any" : "interpreter|cil";
        DialectDefinitionSliceParseErrors.Fail($"Backend target must be one of: {expected}.", token);
        return DialectBackendTarget.Any;
    }

    private void RequireDirectiveEnd(string error)
    {
        if (IsEnd())
            return;

        if (!TryReadTag(DialectLexemeTags.NewLine))
            DialectDefinitionSliceParseErrors.Fail(error, Current());

        SkipNewLines();
    }

    private void SkipNewLines()
    {
        while (TryReadTag(DialectLexemeTags.NewLine))
        {
        }
    }

    private string ReadIdentifier(string error)
    {
        var token = ReadAny(error);
        if (!DialectLexemeTags.IsTag(token, DialectLexemeTags.Identifier))
            DialectDefinitionSliceParseErrors.Fail(error, token);

        return token.Text;
    }

    private string ReadStringLiteral(string error)
    {
        var token = ReadAny(error);
        if (!DialectLexemeTags.IsTag(token, DialectLexemeTags.StringLiteral))
            DialectDefinitionSliceParseErrors.Fail(error, token);

        return token.Text[1..^1];
    }

    private void ExpectTag(string tag, string error)
    {
        var token = ReadAny(error);
        if (!DialectLexemeTags.IsTag(token, tag))
            DialectDefinitionSliceParseErrors.Fail(error, token);
    }

    private bool TryReadTag(string tag)
    {
        if (IsEnd())
            return false;

        if (!DialectLexemeTags.IsTag(tokens[_position], tag))
            return false;

        _position++;
        return true;
    }

    private LexemeValue ReadAny(string error)
    {
        if (IsEnd())
            DialectDefinitionSliceParseErrors.Fail(error, tokens.LastOrDefault());

        return tokens[_position++];
    }

    private LexemeValue? Current() => IsEnd() ? tokens.LastOrDefault() : tokens[_position];

    private bool IsEnd() => _position >= tokens.Count;
}
