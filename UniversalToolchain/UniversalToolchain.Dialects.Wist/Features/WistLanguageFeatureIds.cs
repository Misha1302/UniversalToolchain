using UniversalToolchain.Features.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Features;

/// <summary>
///     Defines stable user-facing feature identifiers for the Wist reference language.
/// </summary>
public static class WistLanguageFeatureIds
{
    public static readonly LanguageFeatureId StandardNumbers = new("StandardNumbers");
    public static readonly LanguageFeatureId NativeNumbers = new("NativeNumbers");
    public static readonly LanguageFeatureId ArithmeticExpressions = new("ArithmeticExpressions");
    public static readonly LanguageFeatureId BooleanLogic = new("BooleanLogic");
    public static readonly LanguageFeatureId ComparisonLogic = new("ComparisonLogic");
    public static readonly LanguageFeatureId EqualityLogic = new("EqualityLogic");
    public static readonly LanguageFeatureId Variables = new("Variables");
    public static readonly LanguageFeatureId Scopes = new("Scopes");
    public static readonly LanguageFeatureId Loops = new("Loops");
    public static readonly LanguageFeatureId Labels = new("Labels");
    public static readonly LanguageFeatureId Comments = new("Comments");
    public static readonly LanguageFeatureId SemicolonAsNewLine = new("SemicolonAsNewLine");
    public static readonly LanguageFeatureId CSharpInterop = new("CSharpInterop");
}
