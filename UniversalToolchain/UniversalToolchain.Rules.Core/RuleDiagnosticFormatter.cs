using System.Text;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Rules.Core;

public static class RuleDiagnosticFormatter
{
    public static string FormatDeterministic(IReadOnlyList<RuleDiagnostic> diagnostics)
    {
        var builder = new StringBuilder();

        foreach (var diagnostic in diagnostics
                     .OrderBy(static x => x.Span?.SourceName ?? string.Empty, StringComparer.Ordinal)
                     .ThenBy(static x => x.Span?.StartLine ?? int.MinValue)
                     .ThenBy(static x => x.Span?.StartColumn ?? int.MinValue)
                     .ThenBy(static x => x.Code, StringComparer.Ordinal)
                     .ThenBy(static x => x.Message, StringComparer.Ordinal))
        {
            builder.AppendLine(FormatDiagnostic(diagnostic));

            foreach (var hint in diagnostic.Hints)
            {
                builder.AppendLine($"  hint: {hint.Message}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatDiagnostic(RuleDiagnostic diagnostic)
    {
        var severity = diagnostic.Severity.ToString().ToLowerInvariant();
        if (diagnostic.Span == null)
        {
            return $"{severity} {diagnostic.Code}: {diagnostic.Message}";
        }

        return $"{diagnostic.Span.SourceName}({diagnostic.Span.StartLine},{diagnostic.Span.StartColumn}) {severity} {diagnostic.Code}: {diagnostic.Message}";
    }
}
