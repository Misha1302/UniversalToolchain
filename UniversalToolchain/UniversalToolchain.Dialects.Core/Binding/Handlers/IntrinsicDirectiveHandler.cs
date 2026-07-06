using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class IntrinsicDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 20;

    public string Name => "Intrinsic";

    public void Apply(DialectDirectiveBindingContext context)
    {
        var intrinsicDirectives = DialectSemanticNormalization.NormalizeIntrinsicRules(
            context.IntrinsicDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Allowed,
            context.DiagnosticsList,
            context.DirectiveContext.IntrinsicContradictionCode);

        context.SetIntrinsicPolicy(new IntrinsicPolicy(
            intrinsicDirectives.Where(x => x.Allowed).Select(FormatRuleName),
            intrinsicDirectives.Where(x => !x.Allowed).Select(FormatRuleName)));
    }

    private static string FormatRuleName(IntrinsicBuildDirective directive) => DialectDirectiveHandlerContext.FormatRuleName(directive.Name, directive.Target);
}
