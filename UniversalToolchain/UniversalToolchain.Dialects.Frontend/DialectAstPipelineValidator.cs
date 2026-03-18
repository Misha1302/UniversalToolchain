using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstPipelineValidator
{
    public static AstNode Validate(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        DialectDslAstValidator.Validate(astRoot);
        return astRoot;
    }
}

public static class DialectDslAstValidator
{
    public static DialectDocumentAstNode Validate(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        if (astRoot.Children.Count != 1 || astRoot.Children[0] is not DialectDocumentAstNode)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect AST must contain exactly one DialectDocumentAstNode root.", astRoot.Children.FirstOrDefault()?.LexemeValue);
        }

        var document = (DialectDocumentAstNode)astRoot.Children[0];
        ValidateDocument(document);
        return document;
    }

    private static void ValidateDocument(DialectDocumentAstNode document)
    {
        if (document.Children.Count == 0)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect document must contain a declaration node.", null);
        }

        if (document.Children[0] is not DialectDeclarationAstNode)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect document must start with a declaration node.", document.Children[0].LexemeValue);
        }

        var declaration = (DialectDeclarationAstNode)document.Children[0];
        ValidateDeclaration(declaration);

        foreach (var node in document.Children.Skip(1))
        {
            if (node is not DialectDirectiveAstNode)
            {
                DialectDefinitionSliceParseErrors.Fail(
                    $"Dialect document contains an unexpected child node of type '{node.NodeType.GetName()}'.",
                    node.LexemeValue ?? node.Children.FirstOrDefault()?.LexemeValue);
            }

            ValidateDirectiveShape((DialectDirectiveAstNode)node);
        }

        ValidateSemanticPolicies(document.Directives);
    }

    private static void ValidateDeclaration(DialectDeclarationAstNode declaration)
    {
        if (declaration.Children.Count != 1 || declaration.Children[0] is not IdentifierValueAstNode)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect declaration must contain exactly one identifier child.", declaration.LexemeValue);
        }

        ValidateIdentifier((IdentifierValueAstNode)declaration.Children[0], "Dialect name must not be empty.");
    }

    private static void ValidateDirectiveShape(DialectDirectiveAstNode directive)
    {
        var descriptor = DialectDirectiveDescriptors.Get(directive.DirectiveKind);
        switch (descriptor.ArgumentShape)
        {
            case DialectDirectiveArgumentShape.IdentifierList:
                if (directive.Children.Count != 1 || directive.Children[0] is not IdentifierListAstNode)
                {
                    DialectDefinitionSliceParseErrors.Fail(
                        $"Directive '{descriptor.Keyword}' must contain exactly one identifier-list child.",
                        directive.LexemeValue ?? directive.Children.FirstOrDefault()?.LexemeValue);
                }

                var listNode = (IdentifierListAstNode)directive.Children[0];
                if (listNode.Identifiers.Count == 0)
                {
                    DialectDefinitionSliceParseErrors.Fail($"Directive '{descriptor.Keyword}' must contain at least one identifier.", directive.LexemeValue);
                }

                foreach (var identifier in listNode.Identifiers)
                {
                    ValidateIdentifier(identifier, $"Directive '{descriptor.Keyword}' contains an empty identifier.");
                }

                ValidateNoDuplicates(listNode.Identifiers.Select(x => x.Identifier), $"Directive '{descriptor.Keyword}' contains duplicate identifiers.", directive.LexemeValue);
                break;

            case DialectDirectiveArgumentShape.Identifier:
                if (directive.Children.Count != 1 || directive.Children[0] is not IdentifierValueAstNode)
                {
                    DialectDefinitionSliceParseErrors.Fail(
                        $"Directive '{descriptor.Keyword}' must contain exactly one identifier child.",
                        directive.LexemeValue ?? directive.Children.FirstOrDefault()?.LexemeValue);
                }

                ValidateIdentifier((IdentifierValueAstNode)directive.Children[0], $"Directive '{descriptor.Keyword}' must not be empty.");
                break;

            default:
                Thrower.InvalidOpEx($"Unsupported directive argument shape '{descriptor.ArgumentShape}'.");
                break;
        }
    }

    private static void ValidateSemanticPolicies(IReadOnlyList<DialectDirectiveAstNode> directives)
    {
        var seenSecurity = false;
        var useModules = new HashSet<string>(StringComparer.Ordinal);
        var excludeModules = new HashSet<string>(StringComparer.Ordinal);
        var requiresModules = new HashSet<string>(StringComparer.Ordinal);
        var beforeModules = new HashSet<string>(StringComparer.Ordinal);
        var afterModules = new HashSet<string>(StringComparer.Ordinal);
        var backends = new HashSet<string>(StringComparer.Ordinal);
        var capabilities = new HashSet<string>(StringComparer.Ordinal);
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        var forbidden = new HashSet<string>(StringComparer.Ordinal);
        var enabled = new HashSet<string>(StringComparer.Ordinal);
        var disabled = new HashSet<string>(StringComparer.Ordinal);

        foreach (var directive in directives)
        {
            switch (directive)
            {
                case UseModulesDirectiveAstNode useDirective:
                    AddMany(useModules, useDirective.Identifiers.Identifiers.Select(x => x.Identifier), "Duplicate use module is not allowed.", useDirective.LexemeValue);
                    break;

                case ExcludeModulesDirectiveAstNode excludeDirective:
                    AddMany(excludeModules, excludeDirective.Identifiers.Identifiers.Select(x => x.Identifier), "Duplicate exclude module is not allowed.", excludeDirective.LexemeValue);
                    break;

                case RequiresModulesDirectiveAstNode requiresDirective:
                    AddMany(requiresModules, requiresDirective.Identifiers.Identifiers.Select(x => x.Identifier), "Duplicate requires module is not allowed.", requiresDirective.LexemeValue);
                    break;

                case BeforeModulesDirectiveAstNode beforeDirective:
                    AddMany(beforeModules, beforeDirective.Identifiers.Identifiers.Select(x => x.Identifier), "Duplicate before module is not allowed.", beforeDirective.LexemeValue);
                    break;

                case AfterModulesDirectiveAstNode afterDirective:
                    AddMany(afterModules, afterDirective.Identifiers.Identifiers.Select(x => x.Identifier), "Duplicate after module is not allowed.", afterDirective.LexemeValue);
                    break;

                case BackendDirectiveAstNode backendDirective:
                    AddMany(backends, backendDirective.Identifiers.Identifiers.Select(x => x.Identifier), "Duplicate backend identifier is not allowed.", backendDirective.LexemeValue);
                    break;

                case CapabilityDirectiveAstNode capabilityDirective:
                    AddMany(capabilities, capabilityDirective.Identifiers.Identifiers.Select(x => x.Identifier), "Duplicate capability identifier is not allowed.", capabilityDirective.LexemeValue);
                    break;

                case AllowIntrinsicDirectiveAstNode allowDirective:
                    AddSingle(allowed, allowDirective.Identifier.Identifier, "Duplicate allow intrinsic directive is not allowed.", allowDirective.LexemeValue);
                    if (forbidden.Contains(allowDirective.Identifier.Identifier))
                    {
                        DialectDefinitionSliceParseErrors.Fail(
                            $"Intrinsic '{allowDirective.Identifier.Identifier}' cannot be both allowed and forbidden.",
                            allowDirective.LexemeValue);
                    }

                    break;

                case ForbidIntrinsicDirectiveAstNode forbidDirective:
                    AddSingle(forbidden, forbidDirective.Identifier.Identifier, "Duplicate forbid intrinsic directive is not allowed.", forbidDirective.LexemeValue);
                    if (allowed.Contains(forbidDirective.Identifier.Identifier))
                    {
                        DialectDefinitionSliceParseErrors.Fail(
                            $"Intrinsic '{forbidDirective.Identifier.Identifier}' cannot be both allowed and forbidden.",
                            forbidDirective.LexemeValue);
                    }

                    break;

                case EnableIntrinsicDirectiveAstNode enableDirective:
                    AddSingle(enabled, enableDirective.Identifier.Identifier, "Duplicate enable directive is not allowed.", enableDirective.LexemeValue);
                    if (disabled.Contains(enableDirective.Identifier.Identifier))
                    {
                        DialectDefinitionSliceParseErrors.Fail(
                            $"Intrinsic '{enableDirective.Identifier.Identifier}' cannot be both enabled and disabled.",
                            enableDirective.LexemeValue);
                    }

                    break;

                case DisableIntrinsicDirectiveAstNode disableDirective:
                    AddSingle(disabled, disableDirective.Identifier.Identifier, "Duplicate disable directive is not allowed.", disableDirective.LexemeValue);
                    if (enabled.Contains(disableDirective.Identifier.Identifier))
                    {
                        DialectDefinitionSliceParseErrors.Fail(
                            $"Intrinsic '{disableDirective.Identifier.Identifier}' cannot be both enabled and disabled.",
                            disableDirective.LexemeValue);
                    }

                    break;

                case SecurityDirectiveAstNode securityDirective:
                    if (seenSecurity)
                    {
                        DialectDefinitionSliceParseErrors.Fail("Security directive can only be declared once.", securityDirective.LexemeValue);
                    }

                    seenSecurity = true;
                    break;
            }
        }

        foreach (var conflict in useModules.Intersect(excludeModules, StringComparer.Ordinal))
        {
            DialectDefinitionSliceParseErrors.Fail($"Module '{conflict}' cannot appear in both use and exclude directives.", null);
        }
    }

    private static void ValidateIdentifier(IdentifierValueAstNode identifier, string message)
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

    private static void ValidateNoDuplicates(IEnumerable<string> values, string message, LexemeValue? token)
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

    private static void AddMany(HashSet<string> set, IEnumerable<string> values, string duplicateMessage, LexemeValue? token)
    {
        foreach (var value in values)
        {
            AddSingle(set, value, duplicateMessage, token);
        }
    }

    private static void AddSingle(HashSet<string> set, string value, string duplicateMessage, LexemeValue? token)
    {
        if (!set.Add(value))
        {
            DialectDefinitionSliceParseErrors.Fail(duplicateMessage, token);
        }
    }
}
