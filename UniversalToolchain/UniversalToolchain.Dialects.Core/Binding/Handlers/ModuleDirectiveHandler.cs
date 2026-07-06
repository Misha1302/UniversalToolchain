using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class ModuleDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 0;

    public string Name => "Module";

    public void Apply(DialectDirectiveBindingContext context)
    {
        var activeModules = DialectSemanticNormalization.NormalizeActiveModules(
            context.UseModules,
            context.ExcludeModules,
            context.DiagnosticsList,
            context.DirectiveContext.ModuleConflictCode);

        context.SetModulePolicy(new ModulePolicy(
            activeModules,
            context.ExcludeModules.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)));
    }
}
