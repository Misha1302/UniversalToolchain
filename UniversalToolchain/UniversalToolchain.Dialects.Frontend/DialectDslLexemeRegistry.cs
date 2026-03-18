using BasicCore.Registration;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslLexemeRegistry
{
    public static IReadOnlyList<LexemeRegistration> CreateRegistrations(DialectDslRegistry registry)
    {
        if (registry == null)
        {
            Thrower.ArgumentNull(nameof(registry));
        }

        var registrations = new List<LexemeRegistration>
        {
            new($@"\b{DialectDslKeywords.Dialect}\b", DialectLexemeTags.DialectKeyword)
        };

        registrations.AddRange(registry.DirectiveFeatures.Select(x => new LexemeRegistration($@"\b{x.Keyword}\b", x.LexemeTag)));
        registrations.Add(new LexemeRegistration(@",", DialectLexemeTags.CommaToken));
        registrations.Add(new LexemeRegistration(@"\r?\n", DialectLexemeTags.NewLine));
        registrations.Add(new LexemeRegistration(@"[A-Za-z_][A-Za-z0-9_\.-]*", DialectLexemeTags.Identifier));
        registrations.Add(new LexemeRegistration(@"[ \t]+", "Whitespace", Ignore: true));
        return registrations;
    }
}
