using System.Text;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Applies structured runtime-profile defaults using the Wist dialect DSL surface.
/// The generic integration applicator targets the canonical v1 parser syntax; Wist owns
/// a compact comma-list syntax and therefore must render additions at this boundary.
/// </summary>
internal sealed class WistRuntimeProfileApplicator
{
    public RuntimeProfileApplicationResult Apply(
        string sourceText,
        DialectDefinitionSlice source,
        RuntimeProfileDefinition profile,
        RuntimeProfileOverridePolicy overridePolicy)
    {
        sourceText = sourceText.ArgNotNull();
        source = source.ArgNotNull();
        profile = profile.ArgNotNull();

        if (!Enum.IsDefined(overridePolicy))
            Thrower.Argument(nameof(overridePolicy), "Runtime profile override policy is not defined.");

        var diagnostics = new List<DialectDiagnostic>();
        var provenance = new List<RuntimeProfileProvenanceEntry>();
        var additions = new List<string>();

        foreach (var module in profile.DefaultModules)
        {
            if (source.UseModules.Contains(module, StringComparer.Ordinal))
            {
                provenance.Add(SourceEntry("use", module));
                continue;
            }

            if (source.ExcludeModules.Contains(module, StringComparer.Ordinal))
            {
                AddConflict(
                    diagnostics,
                    overridePolicy,
                    $"Profile '{profile.Name}' wants module '{module}', but source excludes it.");
                provenance.Add(new RuntimeProfileProvenanceEntry("use", module, "source-exclude"));
                continue;
            }

            additions.Add($"use {module}");
            provenance.Add(ProfileEntry(profile, "use", module));
        }

        foreach (var backend in profile.DefaultBackends)
        {
            var existing = source.BackendDirectives.FirstOrDefault(x => x.Backend == backend);
            if (existing != null)
            {
                if (!existing.Enabled)
                {
                    AddConflict(
                        diagnostics,
                        overridePolicy,
                        $"Profile '{profile.Name}' wants backend '{DialectBackendSelectorText.ToText(backend)}', but source disables it.");
                }

                provenance.Add(SourceEntry("backend", DialectBackendSelectorText.ToText(backend)));
                continue;
            }

            var backendName = DialectBackendSelectorText.ToText(backend);
            additions.Add($"backend {backendName}");
            provenance.Add(ProfileEntry(profile, "backend", backendName));
        }

        foreach (var optimizer in profile.DefaultOptimizers)
        {
            var existing = source.OptimizerDirectives.FirstOrDefault(
                x => string.Equals(x.Name, optimizer, StringComparison.Ordinal));
            if (existing != null)
            {
                if (!existing.Enabled)
                {
                    AddConflict(
                        diagnostics,
                        overridePolicy,
                        $"Profile '{profile.Name}' wants optimizer '{optimizer}', but source disables it.");
                }

                provenance.Add(SourceEntry("optimizer", optimizer));
                continue;
            }

            additions.Add($"enable {optimizer}");
            provenance.Add(ProfileEntry(profile, "optimizer", optimizer));
        }

        if (profile.DefaultSecurityProfile.HasValue)
        {
            var expected = profile.DefaultSecurityProfile.Value == SecurityProfile.Restricted
                ? DialectSecurityProfile.Restricted
                : DialectSecurityProfile.Trusted;
            var securityName = expected.ToString().ToLowerInvariant();

            if (source.SecurityProfile.HasValue)
            {
                if (source.SecurityProfile.Value != expected)
                {
                    AddConflict(
                        diagnostics,
                        overridePolicy,
                        $"Profile '{profile.Name}' wants security '{securityName}', but source declares '{source.SecurityProfile.Value.ToString().ToLowerInvariant()}'.");
                }

                provenance.Add(SourceEntry("security", securityName));
            }
            else
            {
                additions.Add($"security {securityName}");
                provenance.Add(ProfileEntry(profile, "security", securityName));
            }
        }

        foreach (var capability in profile.DefaultCapabilities)
        {
            var existing = source.CapabilityDirectives.FirstOrDefault(
                x => string.Equals(x.Name, capability.Key, StringComparison.Ordinal));
            var existingValue = existing?.Value ?? false;

            if (existing != null)
            {
                if (existingValue != capability.Value)
                {
                    AddConflict(
                        diagnostics,
                        overridePolicy,
                        $"Profile '{profile.Name}' wants capability '{capability.Key}={capability.Value.ToString().ToLowerInvariant()}', but source declares '{existingValue.ToString().ToLowerInvariant()}'.");
                }

                provenance.Add(SourceEntry("capability", capability.Key));
                continue;
            }

            // In the Wist DSL capability presence means true; absence means false.
            if (capability.Value)
                additions.Add($"capability {capability.Key}");

            provenance.Add(ProfileEntry(profile, "capability", capability.Key));
        }

        return new RuntimeProfileApplicationResult(
            profile,
            BuildSource(sourceText, additions),
            diagnostics,
            provenance);
    }

    private static RuntimeProfileProvenanceEntry SourceEntry(string kind, string name) =>
        new(kind, name, "source");

    private static RuntimeProfileProvenanceEntry ProfileEntry(
        RuntimeProfileDefinition profile,
        string kind,
        string name) =>
        new(kind, name, $"profile:{profile.Name}");

    private static void AddConflict(
        ICollection<DialectDiagnostic> diagnostics,
        RuntimeProfileOverridePolicy overridePolicy,
        string message)
    {
        diagnostics.Add(
            new DialectDiagnostic(
                "R301",
                message,
                overridePolicy == RuntimeProfileOverridePolicy.StrictNoConflicts
                    ? DialectDiagnosticSeverity.Error
                    : DialectDiagnosticSeverity.Info));
    }

    private static string BuildSource(string sourceText, IReadOnlyCollection<string> additions)
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
