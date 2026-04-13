using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class BackendDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 0;

    public string Name => "Backend";

    public void Apply(IDialectBindingSource source, DialectDefinitionBuilder builder, List<DialectDiagnostic> diagnostics)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var context = DialectDirectiveHandlerContext.FromInputKind(source.InputKind);
        var backendMap = DialectSemanticNormalization.NormalizeBackendRules(
            source.BackendDirectives,
            x => x.Backend,
            x => x.Enabled,
            diagnostics,
            context.BackendContradictionCode);

        builder.SetBackendPolicy(new BackendPolicy(
            backendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default),
            backendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default)));
    }
}
