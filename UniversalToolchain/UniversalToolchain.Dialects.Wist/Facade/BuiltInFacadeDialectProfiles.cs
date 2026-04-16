namespace UniversalToolchain.Dialects.Wist;

internal static class BuiltInFacadeDialectProfiles
{
    public const string SafeDefaultSyntheticSourceName = "wist-facade-safe-default";

    public const string SafeDefaultText = """
                                          dialect PricingRestricted
                                          use Identifier,NativeTypes,Scopes,Variables,Whitespaces
                                          exclude Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Labels,Loops,ParametersSetter,SemicolonAsNewLine
                                          backend cil,interpreter
                                          enable ArithmeticOptimization
                                          enable EGraphOptimization
                                          enable NativeCilOptimization
                                          enable NativeTypesOptimization
                                          security restricted
                                          """;

    public const string TrustedDefaultSyntheticSourceName = "wist-facade-default";

    public const string TrustedDefaultText = """
                                             dialect FullDefault
                                             use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
                                             backend cil,interpreter
                                             enable BooleanOptimization
                                             enable ComparisonIntrinsicOptimization
                                             enable LocalVariablesOptimization
                                             security trusted
                                             capability unsafe-interop
                                             """;
}
