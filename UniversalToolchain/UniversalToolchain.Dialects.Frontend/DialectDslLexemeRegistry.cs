using BasicCore.Registration;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslLexemeRegistry
{
    private static readonly Lazy<IReadOnlyList<LexemeRegistration>> _registrations = new(BuildRegistrations);

    public static IReadOnlyList<LexemeRegistration> Registrations => _registrations.Value;

    private static IReadOnlyList<LexemeRegistration> BuildRegistrations()
    {
        var registrations = new List<LexemeRegistration>
        {
            new($@"\b{DialectDslKeywords.Dialect}\b", DialectLexemeTags.DialectKeyword)
        };

        registrations.AddRange(DialectDslFeatureCatalog.Features.Select(x => new LexemeRegistration($@"\b{x.Keyword}\b", x.LexemeTag)));
        registrations.Add(new LexemeRegistration(@",", DialectLexemeTags.CommaToken));
        registrations.Add(new LexemeRegistration(@"\r?\n", DialectLexemeTags.NewLine));
        registrations.Add(new LexemeRegistration(@"[A-Za-z_][A-Za-z0-9_\.-]*", DialectLexemeTags.Identifier));
        registrations.Add(new LexemeRegistration(@"[ \t]+", "Whitespace", Ignore: true));
        return registrations;
    }
}
