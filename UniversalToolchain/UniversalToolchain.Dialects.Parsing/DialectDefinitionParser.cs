using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
///     Deterministic parser for v1 dialect DSL.
/// </summary>
public sealed class DialectDefinitionParser : IDialectDefinitionParser
{
    public DialectParseResult Parse(string sourceText)
    {
        if (sourceText == null)
            Thrower.ArgumentNull(nameof(sourceText));

        var diagnostics = new List<DialectDiagnostic>();
        var tokens = DialectLexer.Lex(sourceText, diagnostics);
        var state = new ParserState(tokens, diagnostics);
        var document = ParseDocument(state);

        if (state.Diagnostics.Any(x => x.Severity == DialectDiagnosticSeverity.Error))
            return new DialectParseResult(null, state.Diagnostics);

        return new DialectParseResult(document, state.Diagnostics);
    }

    private static DialectSyntaxDocument? ParseDocument(ParserState state)
    {
        state.SkipNewLines();

        state.ExpectKeyword("dialect");
        var name = state.ExpectIdentifier();

        string? version = null;
        if (state.MatchKeyword("version"))
            version = state.ExpectString();

        state.ExpectLineEnd();

        var useModules = new List<string>();
        var excludeModules = new List<string>();
        var orderRules = new List<OrderRule>();
        var backendDirectives = new List<BackendDirectiveSyntax>();
        var intrinsicDirectives = new List<IntrinsicDirectiveSyntax>();
        var optimizerDirectives = new List<OptimizerDirectiveSyntax>();
        var capabilities = new List<KeyValuePair<string, bool>>();
        SecurityProfile? securityProfile = null;

        var seenUse = new HashSet<string>(StringComparer.Ordinal);
        var seenExclude = new HashSet<string>(StringComparer.Ordinal);
        var seenBackend = new Dictionary<DialectBackendTarget, bool>();
        var seenCapabilities = new HashSet<string>(StringComparer.Ordinal);
        var seenSecurity = false;

        while (!state.IsEnd)
        {
            state.SkipNewLines();
            if (state.IsEnd)
                break;

            if (state.MatchKeyword("use"))
            {
                var module = state.ExpectIdentifier();
                if (!string.IsNullOrWhiteSpace(module))
                {
                    if (!seenUse.Add(module))
                        state.AddError("P100", $"Duplicate use directive for module '{module}'.");

                    if (seenExclude.Contains(module))
                        state.AddError("P101", $"Module '{module}' cannot be both used and excluded.");

                    useModules.Add(module);
                }

                state.ExpectLineEnd();
                continue;
            }

            if (state.MatchKeyword("exclude"))
            {
                var module = state.ExpectIdentifier();
                if (!string.IsNullOrWhiteSpace(module))
                {
                    if (!seenExclude.Add(module))
                        state.AddError("P102", $"Duplicate exclude directive for module '{module}'.");

                    if (seenUse.Contains(module))
                        state.AddError("P101", $"Module '{module}' cannot be both used and excluded.");

                    excludeModules.Add(module);
                }

                state.ExpectLineEnd();
                continue;
            }

            if (state.MatchKeyword("requires") || state.MatchKeyword("before") || state.MatchKeyword("after"))
            {
                var keyword = state.PreviousToken.Text;
                var left = state.ExpectIdentifier();
                state.ExpectArrow();
                var right = state.ExpectIdentifier();

                if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right))
                {
                    var kind = keyword switch
                    {
                        "requires" => OrderRuleKind.Requires,
                        "before" => OrderRuleKind.Before,
                        _ => OrderRuleKind.After
                    };

                    orderRules.Add(new OrderRule(kind, left, right));
                }

                state.ExpectLineEnd();
                continue;
            }

            if (state.MatchKeyword("backend"))
            {
                var backend = state.ExpectBackendTarget(false);
                var enabled = state.ExpectEnableDisable();

                if (seenBackend.TryGetValue(backend, out var existing) && existing != enabled)
                    state.AddError("P103", $"Conflicting backend directive for '{backend.ToString().ToLowerInvariant()}'.");

                if (!seenBackend.ContainsKey(backend))
                    seenBackend[backend] = enabled;
                else if (seenBackend[backend] == enabled)
                    state.AddError("P104", $"Duplicate backend directive for '{backend.ToString().ToLowerInvariant()}'.");

                backendDirectives.Add(new BackendDirectiveSyntax(backend, enabled));
                state.ExpectLineEnd();
                continue;
            }

            if (state.MatchKeyword("allow") || state.MatchKeyword("forbid"))
            {
                var allow = state.PreviousToken.Text == "allow";
                state.ExpectKeyword("intrinsic");
                var intrinsicName = state.ExpectString();
                state.ExpectKeyword("for");
                var target = state.ExpectBackendTarget(true);

                if (!string.IsNullOrWhiteSpace(intrinsicName))
                    intrinsicDirectives.Add(new IntrinsicDirectiveSyntax(intrinsicName, allow, target));

                state.ExpectLineEnd();
                continue;
            }

            if (state.MatchKeyword("enable") || state.MatchKeyword("disable"))
            {
                var enabled = state.PreviousToken.Text == "enable";
                state.ExpectKeyword("optimizer");
                var optimizerName = state.ExpectIdentifier();
                state.ExpectKeyword("for");
                var target = state.ExpectBackendTarget(true);

                if (!string.IsNullOrWhiteSpace(optimizerName))
                    optimizerDirectives.Add(new OptimizerDirectiveSyntax(optimizerName, enabled, target));

                state.ExpectLineEnd();
                continue;
            }

            if (state.MatchKeyword("security"))
            {
                if (seenSecurity)
                    state.AddError("P105", "Security directive can be specified only once.");

                seenSecurity = true;
                securityProfile = state.ExpectSecurityProfile();
                state.ExpectLineEnd();
                continue;
            }

            if (state.MatchKeyword("capability"))
            {
                var capabilityName = state.ExpectIdentifier();
                state.ExpectEquals();
                var value = state.ExpectBoolean();

                if (!string.IsNullOrWhiteSpace(capabilityName))
                {
                    if (!seenCapabilities.Add(capabilityName))
                        state.AddError("P106", $"Duplicate capability directive for '{capabilityName}'.");

                    capabilities.Add(new KeyValuePair<string, bool>(capabilityName, value));
                }

                state.ExpectLineEnd();
                continue;
            }

            state.AddError("P107", $"Unknown directive '{state.Current.Text}'.");
            state.SkipLine();
        }

        if (state.Diagnostics.Any(x => x.Severity == DialectDiagnosticSeverity.Error))
            return null;

        return new DialectSyntaxDocument(
            name,
            version,
            useModules,
            excludeModules,
            orderRules,
            backendDirectives,
            intrinsicDirectives,
            optimizerDirectives,
            securityProfile,
            capabilities);
    }
}