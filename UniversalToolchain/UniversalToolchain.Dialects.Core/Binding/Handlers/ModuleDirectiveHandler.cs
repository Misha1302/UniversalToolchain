using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class ModuleDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 0;

    public string Name => "Module";

    public void Apply(IDialectBindingSource source, DialectDefinitionBuilder builder, List<DialectDiagnostic> diagnostics)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var context = DialectDirectiveHandlerContext.FromInputKind(source.InputKind);
        var activeModules = DialectSemanticNormalization.NormalizeActiveModules(
            source.UseModules,
            source.ExcludeModules,
            diagnostics,
            context.ModuleConflictCode);

        builder.SetModulePolicy(new ModulePolicy(
            activeModules,
            source.ExcludeModules.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)));
    }
}
