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
        new(@"\bintrinsic\b", DialectLexemeTags.IntrinsicKeyword),
        new(@"\bfor\b", DialectLexemeTags.ForKeyword),
        new(@"\benable\b", DialectLexemeTags.EnableKeyword),
        new(@"\bdisable\b", DialectLexemeTags.DisableKeyword),
        new(@"\boptimizer\b", DialectLexemeTags.OptimizerKeyword),
        new(@"\bsecurity\b", DialectLexemeTags.SecurityKeyword),
        new(@"\btrusted\b", DialectLexemeTags.TrustedKeyword),
        new(@"\brestricted\b", DialectLexemeTags.RestrictedKeyword),
        new(@"\bcapability\b", DialectLexemeTags.CapabilityKeyword),
        new(@"\btrue\b", DialectLexemeTags.TrueKeyword),
        new(@"\bfalse\b", DialectLexemeTags.FalseKeyword),
        new(@"\binterpreter\b", DialectLexemeTags.InterpreterKeyword),
        new(@"\bcil\b", DialectLexemeTags.CilKeyword),
        new(@"\bany\b", DialectLexemeTags.AnyKeyword),
        new(@"\-\>", DialectLexemeTags.ArrowToken),
        new(@"\=", DialectLexemeTags.EqualsToken),
        new("\"([^\"\\\\]|\\\\.)*\"", DialectLexemeTags.StringLiteral),
        new(@"\r?\n", DialectLexemeTags.NewLine),
        new(@"[A-Za-z_][A-Za-z0-9_\.-]*", DialectLexemeTags.Identifier),
        new(@"[ \t]+", "Whitespace", Ignore: true)
    ];
}
