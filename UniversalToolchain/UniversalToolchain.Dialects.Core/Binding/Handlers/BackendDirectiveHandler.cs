using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class BackendDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 10;

    public string Name => "Backend";

    public void Apply(DialectDirectiveBindingContext context)
    {
        var backendMap = DialectSemanticNormalization.NormalizeBackendRules(
            context.BackendDirectives,
            x => x.Backend,
            x => x.Enabled,
            context.DiagnosticsList,
            context.DirectiveContext.BackendContradictionCode);

        context.SetBackendPolicy(new BackendPolicy(
            backendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default),
            backendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default)));
    }
}
