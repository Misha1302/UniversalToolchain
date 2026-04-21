namespace Wistc;

internal sealed class WistCliCustomizedDialectBuilder
{
    private static readonly string[] DefaultModules =
    [
        "Whitespaces",
        "SemicolonAsNewLine",
        "Comments",
        "Numbers",
        "Identifier",
        "Arithmetic",
        "Equality",
        "Conditions",
        "Loops",
        "Variables",
        "Scopes",
        "Labels",
        "InternalPreprocessorLexemes",
        "CSharpInterop"
    ];

    public string Build(WistCliCustomizationRequest request)
    {
        request = request.ArgNotNull();

        var modules = DefaultModules.ToList();

        if (request.UseNativeMath)
            modules.Add("NativeTypes");

        modules.AddRange(request.IncludeModules);

        if (request.ExcludeModules.Count > 0)
        {
            var excluded = new HashSet<string>(request.ExcludeModules, StringComparer.OrdinalIgnoreCase);
            modules = modules.Where(x => !excluded.Contains(x)).ToList();
        }

        modules = modules.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return $"""
                dialect CliCustomized
                use {string.Join(",", modules)}
                enable LocalVariablesOptimization
                backend cil,interpreter
                """;
    }
}
