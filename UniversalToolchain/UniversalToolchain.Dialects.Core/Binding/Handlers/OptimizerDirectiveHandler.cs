using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class OptimizerDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 0;

    public string Name => "Optimizer";

    public void Apply(IDialectBindingSource source, DialectDefinitionBuilder builder, List<DialectDiagnostic> diagnostics)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var context = DialectDirectiveHandlerContext.FromInputKind(source.InputKind);
        var optimizerDirectives = DialectSemanticNormalization.NormalizeOptimizerRules(
            source.OptimizerDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Enabled,
            diagnostics,
            context.OptimizerContradictionCode);

        builder.SetOptimizerPolicy(new OptimizerPolicy(
            optimizerDirectives.Where(x => x.Enabled).Select(FormatRuleName),
            optimizerDirectives.Where(x => !x.Enabled).Select(FormatRuleName)));
    }

    private static string FormatRuleName(OptimizerBuildDirective directive)
    {
        return DialectDirectiveHandlerContext.FormatRuleName(directive.Name, directive.Target);
    }
}
