namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Stable descriptor names exposed to the dialect-definition DSL for real Wist integration.
/// </summary>
public static class WistDialectCatalogNames
{
    public static class Modules
    {
        public const string Arithmetic = "Arithmetic";
        public const string BooleanConditions = "BooleanConditions";
        public const string Comments = "Comments";
        public const string ComparisonConditions = "ComparisonConditions";
        public const string Conditions = "Conditions";
        public const string CSharpInterop = "CSharpInterop";
        public const string Equality = "Equality";
        public const string Identifier = "Identifier";
        public const string InternalPreprocessorLexemes = "InternalPreprocessorLexemes";
        public const string Labels = "Labels";
        public const string Loops = "Loops";
        public const string NativeTypes = "NativeTypes";
        public const string Numbers = "Numbers";
        public const string ParametersSetter = "ParametersSetter";
        public const string Scopes = "Scopes";
        public const string SemicolonAsNewLine = "SemicolonAsNewLine";
        public const string Variables = "Variables";
        public const string Whitespaces = "Whitespaces";
    }

    public static class Optimizers
    {
        public const string Arithmetic = "ArithmeticOptimization";
        public const string Boolean = "BooleanOptimization";
        public const string ComparisonIntrinsic = "ComparisonIntrinsicOptimization";
        public const string EGraph = "EGraphOptimization";
        public const string LocalVariables = "LocalVariablesOptimization";
        public const string NativeCil = "NativeCilOptimization";
        public const string NativeTypes = "NativeTypesOptimization";
    }

    public static class Capabilities
    {
        public const string UnsafeInterop = "unsafe-interop";
    }
}