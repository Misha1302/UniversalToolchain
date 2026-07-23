using System.Text;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistLegacyDialectAdapter
{
    public static string BuildDialectText(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var aliases = plan.Contributions
            .Select(static x => x.Contribution.Metadata.TryGetValue("wist.moduleAlias", out var alias) ? alias : null)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();
        if (aliases.Length == 0)
            throw new InvalidOperationException("The language plan contains no Wist module contributions.");

        var name = SanitizeDialectName(plan.Definition.Id.Value);
        var backends = string.Join(",", plan.Definition.Backends.Select(static x => x.Value).OrderBy(static x => x, StringComparer.Ordinal));
        return $"dialect {name}{Environment.NewLine}use {string.Join(",", aliases)}{Environment.NewLine}backend {backends}{Environment.NewLine}";
    }

    private static string SanitizeDialectName(string value)
    {
        var builder = new StringBuilder(value.Length + 1);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        if (builder.Length == 0 || !char.IsLetter(builder[0]) && builder[0] != '_')
            builder.Insert(0, '_');
        return builder.ToString();
    }
}
