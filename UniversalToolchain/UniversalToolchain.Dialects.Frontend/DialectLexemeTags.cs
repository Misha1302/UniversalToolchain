using BasicCore.LexerWrapper;

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
    public const string IntrinsicKeyword = "IntrinsicKeyword";
    public const string ForKeyword = "ForKeyword";
    public const string EnableKeyword = "EnableKeyword";
    public const string DisableKeyword = "DisableKeyword";
    public const string OptimizerKeyword = "OptimizerKeyword";
    public const string SecurityKeyword = "SecurityKeyword";
    public const string TrustedKeyword = "TrustedKeyword";
    public const string RestrictedKeyword = "RestrictedKeyword";
    public const string CapabilityKeyword = "CapabilityKeyword";
    public const string TrueKeyword = "TrueKeyword";
    public const string FalseKeyword = "FalseKeyword";
    public const string InterpreterKeyword = "InterpreterKeyword";
    public const string CilKeyword = "CilKeyword";
    public const string AnyKeyword = "AnyKeyword";
    public const string ArrowToken = "ArrowToken";
    public const string EqualsToken = "EqualsToken";
    public const string StringLiteral = "StringLiteral";
    public const string NewLine = "NewLine";
    public const string Identifier = "Identifier";

    public static bool IsTag(LexemeValue? token, string tag)
    {
        return token?.LexemePattern?.LexemeType.GetName() == tag;
    }
}
