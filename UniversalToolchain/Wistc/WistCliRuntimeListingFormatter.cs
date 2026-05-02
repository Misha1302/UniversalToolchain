using System.Text;

namespace Wistc;

internal static class WistCliRuntimeListingFormatter
{
    public static string Format(IRuntimeComponentCatalog catalog)
    {
        catalog = catalog.ArgNotNull();

        var builder = new StringBuilder();
        builder.AppendLine("Available runtime components:");
        builder.AppendLine("=============================");
        AppendSection(builder, "Modules", catalog.GetModulesInDeterministicOrder());
        AppendSection(builder, "Optimizers", catalog.GetOptimizersInDeterministicOrder());
        AppendSection(builder, "Backends", catalog.GetBackendsInDeterministicOrder());

        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string title, IReadOnlyList<RuntimeComponentManifestEntry> entries)
    {
        builder.AppendLine();
        builder.AppendLine($"{title}:");

        if (entries.Count == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }

        foreach (var entry in entries)
        {
            var aliases = entry.Aliases.Count == 0
                ? string.Empty
                : $" | aliases: {string.Join(", ", entry.Aliases)}";

            builder.AppendLine($"  {entry.CanonicalAlias}{aliases} | id: {entry.ComponentId.Value} | assembly: {entry.AssemblySimpleName}");
        }
    }
}