using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class IntrinsicDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 0;

    public string Name => "Intrinsic";

    public void Apply(IDialectBindingSource source, DialectDefinitionBuilder builder, List<DialectDiagnostic> diagnostics)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var context = DialectDirectiveHandlerContext.FromInputKind(source.InputKind);
        var intrinsicDirectives = DialectSemanticNormalization.NormalizeIntrinsicRules(
            source.IntrinsicDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Allowed,
            diagnostics,
            context.IntrinsicContradictionCode);

        builder.SetIntrinsicPolicy(new IntrinsicPolicy(
            intrinsicDirectives.Where(x => x.Allowed).Select(FormatRuleName),
            intrinsicDirectives.Where(x => !x.Allowed).Select(FormatRuleName)));
    }

    private static string FormatRuleName(IntrinsicBuildDirective directive)
    {
        return DialectDirectiveHandlerContext.FormatRuleName(directive.Name, directive.Target);
    }
}
