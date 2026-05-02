using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class BuiltinFunctionCatalog
{
    private readonly IReadOnlySet<string> _availableBackendAliases;
    private readonly IReadOnlySet<LanguageFeatureId> _availableFeatureIds;
    private readonly BuiltinFunctionRuntimeBindingCatalog _runtimeBindingCatalog;
    private readonly CapabilityCatalog _selectedCapabilityCatalog;

    public BuiltinFunctionCatalog(
        CapabilityCatalog selectedCapabilityCatalog,
        SelectedRuntimePlan selectedRuntimePlan)
    {
        ArgumentNullException.ThrowIfNull(selectedCapabilityCatalog);
        ArgumentNullException.ThrowIfNull(selectedRuntimePlan);

        _selectedCapabilityCatalog = selectedCapabilityCatalog;
        _runtimeBindingCatalog = new BuiltinFunctionRuntimeBindingCatalog(selectedCapabilityCatalog.BuiltinFunctionRuntimeBindings);
        _availableFeatureIds = DialectFeatureExplanationProjector.DetermineAvailableFeatureIds(selectedCapabilityCatalog, selectedRuntimePlan);
        _availableBackendAliases = selectedRuntimePlan.EnabledBackends
            .Select(static x => x.CanonicalAlias)
            .ToHashSet(StringComparer.Ordinal);
    }

    public BuiltinFunctionResolution Resolve(
        string name,
        IReadOnlyList<FunctionTypeDescriptor> argumentTypes,
        string backendAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(argumentTypes);
        ArgumentException.ThrowIfNullOrWhiteSpace(backendAlias);

        var diagnostics = new List<ToolchainDiagnostic>();
        var namedDescriptors = _selectedCapabilityCatalog.BuiltinFunctionDescriptors
            .Where(x => string.Equals(x.Name, name, StringComparison.Ordinal))
            .ToList();
        if (namedDescriptors.Count == 0)
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ToolchainDiagnosticCodes.UnknownFunction,
                ToolchainDiagnosticSeverity.Error,
                $"Builtin function '{name}' is not known.",
                null,
                []));
            return new BuiltinFunctionResolution(false, null, null, null, diagnostics);
        }

        var availableFeatureDescriptors = namedDescriptors
            .Where(x => _availableFeatureIds.Contains(x.FeatureId))
            .ToList();
        if (availableFeatureDescriptors.Count == 0)
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ToolchainDiagnosticCodes.FunctionUnavailable,
                ToolchainDiagnosticSeverity.Error,
                $"Builtin function '{name}' is not available in the selected dialect runtime.",
                null,
                []));
            return new BuiltinFunctionResolution(false, null, null, null, diagnostics);
        }

        if (!_availableBackendAliases.Contains(backendAlias))
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ToolchainDiagnosticCodes.FunctionUnsupportedBackend,
                ToolchainDiagnosticSeverity.Error,
                $"Backend '{backendAlias}' is not selected.",
                null,
                []));
            return new BuiltinFunctionResolution(false, null, null, null, diagnostics);
        }

        var backendSupportedDescriptors = availableFeatureDescriptors
            .Where(x => x.SupportedBackendAliases.Contains(backendAlias, StringComparer.Ordinal))
            .ToList();
        if (backendSupportedDescriptors.Count == 0)
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ToolchainDiagnosticCodes.FunctionUnsupportedBackend,
                ToolchainDiagnosticSeverity.Error,
                $"Builtin function '{name}' does not support backend '{backendAlias}'.",
                null,
                []));
            return new BuiltinFunctionResolution(false, null, null, null, diagnostics);
        }

        var arityMatches = backendSupportedDescriptors
            .Where(x => x.Parameters.Count == argumentTypes.Count)
            .ToList();
        if (arityMatches.Count == 0)
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ToolchainDiagnosticCodes.WrongFunctionArgumentCount,
                ToolchainDiagnosticSeverity.Error,
                $"Builtin function '{name}' expects a different argument count.",
                null,
                []));
            return new BuiltinFunctionResolution(false, null, null, null, diagnostics);
        }

        var exactMatches = arityMatches
            .Where(x => x.Parameters.Select(static y => y.Type.Name)
                .SequenceEqual(argumentTypes.Select(static y => y.Name), StringComparer.Ordinal))
            .ToList();
        if (exactMatches.Count == 0)
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ToolchainDiagnosticCodes.WrongFunctionArgumentType,
                ToolchainDiagnosticSeverity.Error,
                $"Builtin function '{name}' does not have an overload for argument types '{string.Join(", ", argumentTypes.Select(static x => x.Name))}'.",
                null,
                []));
            return new BuiltinFunctionResolution(false, null, null, null, diagnostics);
        }

        var descriptor = exactMatches[0];
        var bindings = _runtimeBindingCatalog.FindMatchingBindings(name, argumentTypes)
            .Where(x => _availableFeatureIds.Contains(x.FeatureId))
            .Where(x => x.SupportedBackendAliases.Contains(backendAlias, StringComparer.Ordinal))
            .ToList();
        if (bindings.Count == 0)
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ToolchainDiagnosticCodes.UnknownBinding,
                ToolchainDiagnosticSeverity.Error,
                $"Builtin function '{name}' does not have a runtime binding for backend '{backendAlias}'.",
                null,
                []));
            return new BuiltinFunctionResolution(false, null, null, null, diagnostics);
        }

        if (bindings.Count > 1)
        {
            diagnostics.Add(new ToolchainDiagnostic(
                ToolchainDiagnosticCodes.BindingConflict,
                ToolchainDiagnosticSeverity.Error,
                $"Builtin function '{name}' has multiple runtime bindings for backend '{backendAlias}'.",
                null,
                []));
            return new BuiltinFunctionResolution(false, null, null, null, diagnostics);
        }

        return new BuiltinFunctionResolution(true, descriptor, bindings[0], descriptor.ReturnType, []);
    }
}