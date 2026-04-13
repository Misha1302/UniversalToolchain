using System.Text.RegularExpressions;
using BasicCore.Registration;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslLexemeRegistry
{
    private const string KeywordBoundary = @"[A-Za-z0-9_\.-]";

    public static IReadOnlyList<LexemeRegistration> CreateRegistrations(DialectDslRegistry registry)
    {
        registry = registry.ArgNotNull();

        var registrations = new List<LexemeRegistration>
        {
            new(CreateKeywordPattern(DialectDslKeywords.Dialect), DialectLexemeTags.DialectKeyword)
        };

        registrations.AddRange(registry.DirectiveFeatures.Select(x => new LexemeRegistration(CreateKeywordPattern(x.Keyword), x.LexemeTag)));
        registrations.Add(new LexemeRegistration(@",", DialectLexemeTags.CommaToken));
        registrations.Add(new LexemeRegistration(@"
?
", DialectLexemeTags.NewLine));
        registrations.Add(new LexemeRegistration(@"[A-Za-z_][A-Za-z0-9_\.-]*", DialectLexemeTags.Identifier));
        registrations.Add(new LexemeRegistration(@"[ 	]+", "Whitespace", true));
        return registrations;
    }

    internal static string CreateKeywordPattern(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            Thrower.Argument(nameof(keyword), "Dialect directive keyword must not be empty.");

        return $@"(?<!{KeywordBoundary}){Regex.Escape(keyword)}(?!{KeywordBoundary})";
    }
}