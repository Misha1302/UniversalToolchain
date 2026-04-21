namespace Wistc;

internal sealed record WistCliCustomizationRequest(
    bool UseNativeMath,
    IReadOnlyList<string> IncludeModules,
    IReadOnlyList<string> ExcludeModules)
{
    public bool HasCustomization => UseNativeMath || IncludeModules.Count > 0 || ExcludeModules.Count > 0;

    public static WistCliCustomizationRequest FromOptions(CommonOptions options)
    {
        options = options.ArgNotNull();

        return new WistCliCustomizationRequest(
            options.UseNativeMath,
            Normalize(options.IncludeModules),
            Normalize(options.ExcludeModules));
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string>? modules)
        => (modules ?? [])
            .Select(static x => x?.Trim())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x!)
            .ToList();
}
