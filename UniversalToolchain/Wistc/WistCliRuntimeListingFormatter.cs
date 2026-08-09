using System.Text;
using UniversalToolchain.FeatureSdk;

namespace Wistc;

internal static class WistCliRuntimeListingFormatter
{
    public static string Format(LanguagePackageDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var builder = new StringBuilder();
        builder.AppendLine("Available canonical Wist components:");
        builder.AppendLine("====================================");
        AppendSection(builder, "Modules", descriptor.Contributions, "wist.moduleAlias");
        AppendSection(builder, "Optimizers", descriptor.Contributions, "wist.optimizerAlias");
        return builder.ToString();
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<LanguageContributionDescriptor> contributions,
        string metadataKey)
    {
        var entries = contributions
            .Where(contribution => contribution.Metadata.ContainsKey(metadataKey))
            .OrderBy(contribution => contribution.Metadata[metadataKey], StringComparer.Ordinal)
            .ToArray();
        builder.AppendLine();
        builder.AppendLine($"{title}:");
        if (entries.Length == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }
        foreach (var entry in entries)
            builder.AppendLine($"  {entry.Metadata[metadataKey]} | id: {entry.Id.Value}");
    }
}
