using System.Text;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Applies runtime profile defaults to dialect DSL source while preserving explicit source intent.
/// </summary>
public sealed class RuntimeProfileApplicator
{
    private readonly IDialectDefinitionParser _parser;

    public RuntimeProfileApplicator()
        : this(new DialectDefinitionParser())
    {
    }

    public RuntimeProfileApplicator(IDialectDefinitionParser parser)
    {
        _parser = parser.ArgNotNull();
    }

    public RuntimeProfileApplicationResult Apply(
        string sourceText,
        RuntimeProfileDefinition profile,
        RuntimeProfileOverridePolicy overridePolicy = RuntimeProfileOverridePolicy.ExplicitSourceWins)
    {
        sourceText = sourceText.ArgNotNull();
        profile = profile.ArgNotNull();

        if (!Enum.IsDefined(overridePolicy))
            Thrower.Argument(nameof(overridePolicy), "Runtime profile override policy is not defined.");

        var parsed = _parser.Parse(sourceText);
        if (parsed.Document == null)
            return new RuntimeProfileApplicationResult(profile, sourceText, parsed.Diagnostics, []);

        var diagnostics = new List<DialectDiagnostic>(parsed.Diagnostics);
        var provenance = new List<RuntimeProfileProvenanceEntry>();
        var additions = new List<string>();
        var document = parsed.Document;

        foreach (var module in profile.DefaultModules)
        {
            if (document.UseModules.Contains(module, StringComparer.Ordinal))
            {
                provenance.Add(new RuntimeProfileProvenanceEntry("use", module, "source"));
                continue;
            }

            if (document.ExcludeModules.Contains(module, StringComparer.Ordinal))
            {
                AddConflict(diagnostics, overridePolicy, $"Profile '{profile.Name}' wants module '{module}', but source excludes it.");
                provenance.Add(new RuntimeProfileProvenanceEntry("use", module, "source-exclude"));
                continue;
            }

            additions.Add($"use {module}");
            provenance.Add(new RuntimeProfileProvenanceEntry("use", module, $"profile:{profile.Name}"));
        }

        foreach (var backend in profile.DefaultBackends)
        {
            var existing = document.BackendDirectives.FirstOrDefault(x => x.Backend == backend);
            if (existing != null)
            {
                if (!existing.Enabled)
                    AddConflict(
                        diagnostics,
                        overridePolicy,
                        $"Profile '{profile.Name}' wants backend '{DialectBackendSelectorText.ToText(backend)}', but source disables it.");

                provenance.Add(new RuntimeProfileProvenanceEntry("backend", DialectBackendSelectorText.ToText(backend), "source"));
                continue;
            }

            additions.Add($"backend {DialectBackendSelectorText.ToText(backend)}");
            provenance.Add(new RuntimeProfileProvenanceEntry("backend", DialectBackendSelectorText.ToText(backend), $"profile:{profile.Name}"));
        }

        foreach (var optimizer in profile.DefaultOptimizers)
        {
            if (document.OptimizerDirectives.Any(x => string.Equals(x.Name, optimizer, StringComparison.Ordinal)))
            {
                provenance.Add(new RuntimeProfileProvenanceEntry("optimizer", optimizer, "source"));
                continue;
            }

            additions.Add($"enable optimizer {optimizer} for any");
            provenance.Add(new RuntimeProfileProvenanceEntry("optimizer", optimizer, $"profile:{profile.Name}"));
        }

        if (profile.DefaultSecurityProfile.HasValue)
        {
            var securityName = profile.DefaultSecurityProfile.Value.ToString().ToLowerInvariant();
            if (document.SecurityProfile.HasValue)
            {
                if (document.SecurityProfile.Value != profile.DefaultSecurityProfile.Value)
                    AddConflict(
                        diagnostics,
                        overridePolicy,
                        $"Profile '{profile.Name}' wants security '{securityName}', but source declares '{document.SecurityProfile.Value.ToString().ToLowerInvariant()}'.");

                provenance.Add(new RuntimeProfileProvenanceEntry("security", securityName, "source"));
            }
            else
            {
                additions.Add($"security {securityName}");
                provenance.Add(new RuntimeProfileProvenanceEntry("security", securityName, $"profile:{profile.Name}"));
            }
        }

        foreach (var capability in profile.DefaultCapabilities)
        {
            if (document.Capabilities.TryGetValue(capability.Key, out var existing))
            {
                if (existing != capability.Value)
                    AddConflict(
                        diagnostics,
                        overridePolicy,
                        $"Profile '{profile.Name}' wants capability '{capability.Key}={capability.Value.ToString().ToLowerInvariant()}', but source declares '{existing.ToString().ToLowerInvariant()}'.");

                provenance.Add(new RuntimeProfileProvenanceEntry("capability", capability.Key, "source"));
                continue;
            }

            additions.Add($"capability {capability.Key} = {capability.Value.ToString().ToLowerInvariant()}");
            provenance.Add(new RuntimeProfileProvenanceEntry("capability", capability.Key, $"profile:{profile.Name}"));
        }

        return new RuntimeProfileApplicationResult(
            profile,
            BuildSource(sourceText, additions),
            diagnostics,
            provenance);
    }

    private static void AddConflict(
        ICollection<DialectDiagnostic> diagnostics,
        RuntimeProfileOverridePolicy overridePolicy,
        string message)
    {
        var severity = overridePolicy == RuntimeProfileOverridePolicy.StrictNoConflicts
            ? DialectDiagnosticSeverity.Error
            : DialectDiagnosticSeverity.Info;
        diagnostics.Add(new DialectDiagnostic("R301", message, severity));
    }

    private static string BuildSource(string sourceText, IReadOnlyList<string> additions)
    {
        if (additions.Count == 0)
            return sourceText;

        var builder = new StringBuilder(sourceText.TrimEnd());
        builder.AppendLine();
        foreach (var addition in additions)
            builder.AppendLine(addition);

        return builder.ToString();
    }
}
