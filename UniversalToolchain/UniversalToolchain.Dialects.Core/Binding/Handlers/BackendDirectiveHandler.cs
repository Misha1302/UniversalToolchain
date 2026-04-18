using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class BackendDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 10;

    public string Name => "Backend";

    public void Apply(DialectBindingExecutionContext context)
    {
        var backendMap = DialectSemanticNormalization.NormalizeBackendRules(
            context.Source.BackendDirectives,
            x => x.Backend,
            x => x.Enabled,
            context.Diagnostics,
            context.DirectiveContext.BackendContradictionCode);

        context.Builder.SetBackendPolicy(new BackendPolicy(
            backendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default),
            backendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default)));
    }
}