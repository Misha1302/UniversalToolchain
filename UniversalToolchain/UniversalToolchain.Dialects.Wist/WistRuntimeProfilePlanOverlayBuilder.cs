using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistRuntimeProfilePlanOverlayBuilder
{
    public DialectPlanOverlay Build(
        DialectDefinitionSlice source,
        DialectBuildPlan baseline,
        RuntimeProfileDefinition profile,
        RuntimeProfileOverridePolicy overridePolicy)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(profile);
        if (!Enum.IsDefined(overridePolicy))
            throw new ArgumentOutOfRangeException(nameof(overridePolicy));

        var modules = new List<string>();
        var backends = new List<DialectBackendId>();
        var optimizers = new List<OptimizerBuildDirective>();
        var capabilities = new Dictionary<string, bool>(StringComparer.Ordinal);
        var diagnostics = new List<DialectDiagnostic>();
        var provenance = new List<RuntimeProfileProvenanceEntry>();
        SecurityProfile? security = null;

        foreach (var module in profile.DefaultModules)
        {
            if (source.UseModules.Contains(module, StringComparer.Ordinal))
            {
                provenance.Add(Source("use", module));
                continue;
            }
            if (source.ExcludeModules.Contains(module, StringComparer.Ordinal))
            {
                Conflict(diagnostics, overridePolicy, $"Profile '{profile.Name}' wants module '{module}', but source excludes it.");
                provenance.Add(new RuntimeProfileProvenanceEntry("use", module, "source-exclude"));
                continue;
            }
            modules.Add(module);
            provenance.Add(Profile(profile, "use", module));
        }

        foreach (var backend in profile.DefaultBackends)
        {
            var existing = source.BackendDirectives.FirstOrDefault(directive => directive.Backend == backend);
            if (existing != null)
            {
                if (!existing.Enabled)
                    Conflict(diagnostics, overridePolicy, $"Profile '{profile.Name}' wants backend '{backend}', but source disables it.");
                provenance.Add(Source("backend", backend.Value));
                continue;
            }
            if (!baseline.EnabledBackends.Contains(backend))
                backends.Add(backend);
            provenance.Add(Profile(profile, "backend", backend.Value));
        }

        foreach (var optimizer in profile.DefaultOptimizers)
        {
            var existing = source.OptimizerDirectives.FirstOrDefault(directive => StringComparer.Ordinal.Equals(directive.Name, optimizer));
            if (existing != null)
            {
                if (!existing.Enabled)
                    Conflict(diagnostics, overridePolicy, $"Profile '{profile.Name}' wants optimizer '{optimizer}', but source disables it.");
                provenance.Add(Source("optimizer", optimizer));
                continue;
            }
            if (!baseline.OptimizerDirectives.Any(directive => StringComparer.Ordinal.Equals(directive.Name, optimizer) && directive.Enabled))
                optimizers.Add(new OptimizerBuildDirective(optimizer, true, DialectBackendSelector.Any));
            provenance.Add(Profile(profile, "optimizer", optimizer));
        }

        if (profile.DefaultSecurityProfile.HasValue)
        {
            var expected = profile.DefaultSecurityProfile.Value;
            if (baseline.SecurityProfile.HasValue)
            {
                if (baseline.SecurityProfile.Value != expected)
                    Conflict(diagnostics, overridePolicy, $"Profile '{profile.Name}' wants security '{expected}', but source declares '{baseline.SecurityProfile.Value}'.");
                provenance.Add(Source("security", baseline.SecurityProfile.Value.ToString()));
            }
            else
            {
                security = expected;
                provenance.Add(Profile(profile, "security", expected.ToString()));
            }
        }

        foreach (var capability in profile.DefaultCapabilities)
        {
            var existing = source.CapabilityDirectives.FirstOrDefault(directive => StringComparer.Ordinal.Equals(directive.Name, capability.Key));
            if (existing != null)
            {
                if (existing.Value != capability.Value)
                    Conflict(diagnostics, overridePolicy, $"Profile '{profile.Name}' wants capability '{capability.Key}={capability.Value}', but source declares '{existing.Value}'.");
                provenance.Add(Source("capability", capability.Key));
                continue;
            }
            if (!baseline.Capabilities.ContainsKey(capability.Key))
                capabilities.Add(capability.Key, capability.Value);
            provenance.Add(Profile(profile, "capability", capability.Key));
        }

        return new DialectPlanOverlay(modules, backends, optimizers, security, capabilities, diagnostics, provenance);
    }

    private static RuntimeProfileProvenanceEntry Source(string kind, string name) => new(kind, name, "source");
    private static RuntimeProfileProvenanceEntry Profile(RuntimeProfileDefinition profile, string kind, string name) => new(kind, name, $"profile:{profile.Name}");

    private static void Conflict(ICollection<DialectDiagnostic> diagnostics, RuntimeProfileOverridePolicy policy, string message) =>
        diagnostics.Add(new DialectDiagnostic(
            "R301",
            message,
            policy == RuntimeProfileOverridePolicy.StrictNoConflicts ? DialectDiagnosticSeverity.Error : DialectDiagnosticSeverity.Info));
}
