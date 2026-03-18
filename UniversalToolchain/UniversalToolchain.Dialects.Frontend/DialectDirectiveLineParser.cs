using BasicCore.LexerWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveLineParser
{
    public bool TryParse(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        if (line == null)
        {
            Thrower.ArgumentNull(nameof(line));
        }

        if (accumulation == null)
        {
            Thrower.ArgumentNull(nameof(accumulation));
        }

        if (line.Count == 0)
        {
            return true;
        }

        var keyword = line[0].Text;
        if (!DialectDirectiveDescriptors.TryGetByKeyword(keyword, out var descriptor))
        {
            return false;
        }

        switch (descriptor.Kind)
        {
            case DialectDirectiveKind.UseModules:
                accumulation.UseModules.AddRange(ParseIdentifierList(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.ExcludeModules:
                accumulation.ExcludeModules.AddRange(ParseIdentifierList(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.RequiresModules:
                accumulation.RequiresModules.AddRange(ParseIdentifierList(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.BeforeModules:
                accumulation.BeforeModules.AddRange(ParseIdentifierList(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.AfterModules:
                accumulation.AfterModules.AddRange(ParseIdentifierList(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.Backend:
                accumulation.Backends.AddRange(ParseIdentifierList(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.AllowIntrinsic:
                accumulation.AllowedIntrinsics.Add(ParseSingleIdentifier(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.ForbidIntrinsic:
                accumulation.ForbiddenIntrinsics.Add(ParseSingleIdentifier(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.EnableIntrinsic:
                accumulation.EnabledIntrinsics.Add(ParseSingleIdentifier(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.DisableIntrinsic:
                accumulation.DisabledIntrinsics.Add(ParseSingleIdentifier(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.Security:
                if (accumulation.SecurityProfile != null)
                {
                    DialectDefinitionSliceParseErrors.Fail("Security directive can only be declared once.", line[0]);
                }

                accumulation.SecurityProfile = DialectAnnotationValueGuard.ParseSecurityProfile(ParseSingleIdentifier(line, descriptor.Keyword));
                return true;
            case DialectDirectiveKind.Capability:
                accumulation.Capabilities.AddRange(ParseIdentifierList(line, descriptor.Keyword));
                return true;
            default:
                Thrower.InvalidOpEx($"Directive parser does not support directive kind '{descriptor.Kind}'.");
                return false;
        }
    }

    private static string ParseSingleIdentifier(IReadOnlyList<LexemeValue> line, string directiveName)
    {
        if (line.Count != 2 || !DialectLexemeTags.IsTag(line[1], DialectLexemeTags.Identifier))
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' expects exactly one identifier argument.", line.ElementAtOrDefault(1) ?? line[0]);
        }

        return line[1].Text;
    }

    private static IReadOnlyList<string> ParseIdentifierList(IReadOnlyList<LexemeValue> line, string directiveName)
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
