namespace UniversalToolchain.Dialects.Frontend;

public static class DialectLexemeTags
{
    public const string DialectKeyword = "DialectKeyword";
    public const string UseKeyword = "UseKeyword";
    public const string ExcludeKeyword = "ExcludeKeyword";
    public const string RequiresKeyword = "RequiresKeyword";
    public const string BeforeKeyword = "BeforeKeyword";
    public const string AfterKeyword = "AfterKeyword";
    public const string BackendKeyword = "BackendKeyword";
    public const string AllowKeyword = "AllowKeyword";
    public const string ForbidKeyword = "ForbidKeyword";
    public const string EnableKeyword = "EnableKeyword";
    public const string DisableKeyword = "DisableKeyword";
    public const string SecurityKeyword = "SecurityKeyword";
    public const string CapabilityKeyword = "CapabilityKeyword";
    public const string NewLine = "NewLine";
    public const string CommaToken = "CommaToken";
    public const string Identifier = "Identifier";

    public static bool IsTag(LexemeValue? token, string tag) => token?.LexemePattern?.LexemeType.GetName() == tag;
}