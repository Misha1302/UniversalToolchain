using System.Text;
using ExceptionsManager;
using UniversalToolchain.Features.Abstractions;

namespace UniversalToolchain.Features.Core;

public static class DialectFeatureExplanationFormatter
{
    public static string FormatDeterministic(DialectFeatureExplanation explanation)
    {
        explanation = explanation.ArgNotNull();

        var builder = new StringBuilder();
        builder.AppendLine($"Dialect: {explanation.DialectName}");
        builder.AppendLine();

        AppendAvailableFeatures(builder, explanation.AvailableFeatures);
        builder.AppendLine();
        AppendAvailableSymbols(builder, explanation.AvailableSymbols);
        builder.AppendLine();
        AppendUnavailableFeatures(builder, explanation.UnavailableFeatures);
        builder.AppendLine();
        AppendBackends(builder, explanation.BackendSupport);

        return builder.ToString().TrimEnd();
    }

    private static void AppendAvailableFeatures(StringBuilder builder, IReadOnlyList<AvailableLanguageFeature> features)
    {
        builder.AppendLine("Available features:");

        if (features.Count == 0)
        {
            builder.AppendLine("- (none)");
            return;
        }

        foreach (var feature in features.OrderBy(static x => x.Descriptor.FeatureId.Value, StringComparer.Ordinal))
        {
            builder.AppendLine($"- {feature.Descriptor.FeatureId.Value}");
        }
    }

    private static void AppendAvailableSymbols(StringBuilder builder, IReadOnlyList<LanguageFeatureSymbolDescriptor> symbols)
    {
        builder.AppendLine("Available symbols:");

        if (symbols.Count == 0)
        {
            builder.AppendLine("- (none)");
            return;
        }

        foreach (var symbol in symbols
                     .OrderBy(static x => x.Kind.ToString(), StringComparer.Ordinal)
                     .ThenBy(static x => x.Name, StringComparer.Ordinal)
                     .ThenBy(static x => x.Signature, StringComparer.Ordinal)
                     .ThenBy(static x => x.Description, StringComparer.Ordinal))
        {
            builder.AppendLine($"- {FormatSymbol(symbol)}");
        }
    }

    private static void AppendUnavailableFeatures(StringBuilder builder, IReadOnlyList<UnavailableLanguageFeature> features)
    {
        builder.AppendLine("Unavailable features:");

        if (features.Count == 0)
        {
            builder.AppendLine("- (none)");
            return;
        }

        foreach (var feature in features.OrderBy(static x => x.Descriptor.FeatureId.Value, StringComparer.Ordinal))
        {
            builder.AppendLine($"- {feature.Descriptor.FeatureId.Value}: {string.Join("; ", feature.Reasons)}");
        }
    }

    private static void AppendBackends(StringBuilder builder, IReadOnlyList<DialectFeatureBackendSupport> backendSupport)
    {
        builder.AppendLine("Backends:");

        if (backendSupport.Count == 0)
        {
            builder.AppendLine("- (none)");
            return;
        }

        foreach (var backend in backendSupport.OrderBy(static x => x.BackendAlias, StringComparer.Ordinal))
        {
            var supportedFeatures = backend.SupportedFeatures.Count == 0
                ? "(none)"
                : string.Join(", ", backend.SupportedFeatures.Select(static x => x.Value));

            builder.AppendLine($"- {backend.BackendAlias}: {supportedFeatures}");
        }
    }

    private static string FormatSymbol(LanguageFeatureSymbolDescriptor symbol)
    {
        var symbolText = string.IsNullOrWhiteSpace(symbol.Signature)
            ? symbol.Name
            : symbol.Signature;

        return $"{ToSymbolKindText(symbol.Kind)} {symbolText}";
    }

    private static string ToSymbolKindText(LanguageFeatureSymbolKind kind)
    {
        return kind switch
        {
            LanguageFeatureSymbolKind.SyntaxForm => "syntax",
            LanguageFeatureSymbolKind.Function => "function",
            LanguageFeatureSymbolKind.Type => "type",
            LanguageFeatureSymbolKind.RuleForm => "rule",
            LanguageFeatureSymbolKind.Operator => "operator",
            LanguageFeatureSymbolKind.HostBinding => "host-binding",
            _ => kind.ToString().ToLowerInvariant()
        };
    }
}
