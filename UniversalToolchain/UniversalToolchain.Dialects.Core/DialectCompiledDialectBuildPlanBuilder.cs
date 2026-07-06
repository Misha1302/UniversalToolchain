using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Core.Binding.Handlers;
using UniversalToolchain.Dialects.Core.Groups;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
///     Builds a validated and normalized DialectBuildPlan from framework-native compiled DSL output.
/// </summary>
public sealed class DialectCompiledDialectBuildPlanBuilder : IDialectCompiledDialectBuildPlanBuilder
{
    private readonly DialectDirectiveHandlerRegistry _directiveHandlerRegistry;
    private readonly DialectGroupExpander _groupExpander;

    public DialectCompiledDialectBuildPlanBuilder()
        : this(
            new DialectGroupExpander(new EmptyDialectGroupCatalog()),
            DialectDefinitionSemanticBinder.CreateDefaultDirectiveHandlerRegistry())
    {
    }

    public DialectCompiledDialectBuildPlanBuilder(DialectGroupExpander groupExpander)
        : this(groupExpander, DialectDefinitionSemanticBinder.CreateDefaultDirectiveHandlerRegistry())
    {
    }

    public DialectCompiledDialectBuildPlanBuilder(
        DialectGroupExpander groupExpander,
        DialectDirectiveHandlerRegistry directiveHandlerRegistry)
    {
        _groupExpander = groupExpander.ArgNotNull();
        _directiveHandlerRegistry = directiveHandlerRegistry.ArgNotNull();
    }

    public DialectBuildPlan Build(DialectDefinitionSlice compiledDialect)
    {
        compiledDialect = compiledDialect.ArgNotNull();

        var diagnostics = new List<DialectDiagnostic>();
        var source = new CompiledDialectBindingSource(compiledDialect);
        var expandedSource = _groupExpander.Expand(source, diagnostics);

        return DialectDefinitionSemanticBinder.BuildPlanCore(
            expandedSource,
            diagnostics,
            "S105",
            "Order directives contain a cycle involving modules",
            _directiveHandlerRegistry);
    }
}
