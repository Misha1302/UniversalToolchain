using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class OptimizerDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 30;

    public string Name => "Optimizer";

    public void Apply(DialectBindingExecutionContext context)
    {
        var optimizerDirectives = DialectSemanticNormalization.NormalizeOptimizerRules(
            context.Source.OptimizerDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Enabled,
            context.Diagnostics,
            context.DirectiveContext.OptimizerContradictionCode);

        context.Builder.SetOptimizerPolicy(new OptimizerPolicy(
            optimizerDirectives.Where(x => x.Enabled).Select(FormatRuleName),
            optimizerDirectives.Where(x => !x.Enabled).Select(FormatRuleName)));
    }

    private static string FormatRuleName(OptimizerBuildDirective directive) => DialectDirectiveHandlerContext.FormatRuleName(directive.Name, directive.Target);
}