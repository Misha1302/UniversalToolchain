using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class ModuleDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 0;

    public string Name => "Module";

    public void Apply(DialectBindingExecutionContext context)
    {
        var activeModules = DialectSemanticNormalization.NormalizeActiveModules(
            context.Source.UseModules,
            context.Source.ExcludeModules,
            context.Diagnostics,
            context.DirectiveContext.ModuleConflictCode);

        context.Builder.SetModulePolicy(new ModulePolicy(
            activeModules,
            context.Source.ExcludeModules.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)));
    }
}