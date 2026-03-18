using BasicCore.Registration;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslLexemeRegistry
{
    public static IReadOnlyList<LexemeRegistration> Registrations { get; } =
    [
        new(@"\bdialect\b", DialectLexemeTags.DialectKeyword),
        new(@"\buse\b", DialectLexemeTags.UseKeyword),
        new(@"\bexclude\b", DialectLexemeTags.ExcludeKeyword),
        new(@"\brequires\b", DialectLexemeTags.RequiresKeyword),
        new(@"\bbefore\b", DialectLexemeTags.BeforeKeyword),
        new(@"\bafter\b", DialectLexemeTags.AfterKeyword),
        new(@"\bbackend\b", DialectLexemeTags.BackendKeyword),
        new(@"\ballow\b", DialectLexemeTags.AllowKeyword),
        new(@"\bforbid\b", DialectLexemeTags.ForbidKeyword),
        new(@"\benable\b", DialectLexemeTags.EnableKeyword),
        new(@"\bdisable\b", DialectLexemeTags.DisableKeyword),
        new(@"\bsecurity\b", DialectLexemeTags.SecurityKeyword),
        new(@"\bcapability\b", DialectLexemeTags.CapabilityKeyword),
        new(@",", DialectLexemeTags.CommaToken),
        new(@"\r?\n", DialectLexemeTags.NewLine),
        new(@"[A-Za-z_][A-Za-z0-9_\.-]*", DialectLexemeTags.Identifier),
        new(@"[ \t]+", "Whitespace", Ignore: true)
    ];
}
