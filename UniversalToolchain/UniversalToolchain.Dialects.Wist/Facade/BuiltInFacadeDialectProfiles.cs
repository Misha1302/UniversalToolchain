namespace UniversalToolchain.Dialects.Wist;

internal static class BuiltInFacadeDialectProfiles
{
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
