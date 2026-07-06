using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

public sealed class DialectDirectiveBindingContext
{
    internal DialectDirectiveBindingContext(
        IDialectBindingSource source,
        DialectDefinitionBuilder builder,
        List<DialectDiagnostic> diagnostics)
    {
        source = source.ArgNotNull();

        builder = builder.ArgNotNull();

        diagnostics = diagnostics.ArgNotNull();

        Source = source;
        Builder = builder;
        DiagnosticsList = diagnostics;
        DirectiveContext = DialectDirectiveHandlerContext.FromInputKind(source.InputKind);
    }

    internal IDialectBindingSource Source { get; }

    internal DialectDefinitionBuilder Builder { get; }

    public DialectBindingInputKind InputKind => Source.InputKind;

    public string DialectName => Source.Name;

    public string? Version => Source.Version;

    public string? BaseDialectName => Source.BaseDialectName;

    public IReadOnlyList<string> UseModules => Source.UseModules;

    public IReadOnlyList<string> ExcludeModules => Source.ExcludeModules;

    public IReadOnlyList<OrderBindingDirectiveRecord> OrderRules => Source.OrderRules;

    public IReadOnlyList<BackendBindingDirectiveRecord> BackendDirectives => Source.BackendDirectives;

    public IReadOnlyList<IntrinsicBindingDirectiveRecord> IntrinsicDirectives => Source.IntrinsicDirectives;

    public IReadOnlyList<OptimizerBindingDirectiveRecord> OptimizerDirectives => Source.OptimizerDirectives;

    public SecurityProfile? SecurityProfile => Source.SecurityProfile;

    public IReadOnlyList<KeyValuePair<string, bool>> Capabilities => Source.Capabilities;

    public IReadOnlyList<DialectDiagnostic> Diagnostics => DiagnosticsList;

    internal List<DialectDiagnostic> DiagnosticsList { get; }

    public DialectDirectiveHandlerContext DirectiveContext { get; }

    public void AddDiagnostic(DialectDiagnostic diagnostic)
    {
        diagnostic = diagnostic.ArgNotNull();

        DiagnosticsList.Add(diagnostic);
    }

    public void SetModulePolicy(ModulePolicy modulePolicy) => Builder.SetModulePolicy(modulePolicy);

    public void SetBackendPolicy(BackendPolicy backendPolicy) => Builder.SetBackendPolicy(backendPolicy);

    public void SetIntrinsicPolicy(IntrinsicPolicy intrinsicPolicy) => Builder.SetIntrinsicPolicy(intrinsicPolicy);

    public void SetOptimizerPolicy(OptimizerPolicy optimizerPolicy) => Builder.SetOptimizerPolicy(optimizerPolicy);

    public void SetSecurityPolicy(SecurityPolicy? securityPolicy) => Builder.SetSecurityPolicy(securityPolicy);

    public void SetCapabilityPolicy(CapabilityPolicy capabilityPolicy) => Builder.SetCapabilityPolicy(capabilityPolicy);

    public void SetExtension(string key, object value) => Builder.SetExtension(key, value);

    public bool TryGetExtension(string key, out object? value) => Builder.TryGetExtension(key, out value);
}
