using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Core.Binding.Handlers;
using UniversalToolchain.Dialects.Core.Groups;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
///     Default semantic validator and deterministic build-plan builder.
/// </summary>
public sealed class DialectBuildPlanBuilder : IDialectBuildPlanBuilder
{
    private readonly DialectDirectiveHandlerRegistry _directiveHandlerRegistry;
    private readonly DialectGroupExpander _groupExpander;

    public DialectBuildPlanBuilder()
        : this(
            new DialectGroupExpander(new EmptyDialectGroupCatalog()),
            DialectDefinitionSemanticBinder.CreateDefaultDirectiveHandlerRegistry())
    {
    }

    public DialectBuildPlanBuilder(DialectGroupExpander groupExpander)
        : this(groupExpander, DialectDefinitionSemanticBinder.CreateDefaultDirectiveHandlerRegistry())
    {
    }

    public DialectBuildPlanBuilder(
        DialectGroupExpander groupExpander,
        DialectDirectiveHandlerRegistry directiveHandlerRegistry)
    {
        _groupExpander = groupExpander.ArgNotNull();
        _directiveHandlerRegistry = directiveHandlerRegistry.ArgNotNull();
    }

    public DialectBuildPlan Build(DialectSyntaxDocument syntaxDocument)
    {
        syntaxDocument = syntaxDocument.ArgNotNull();

        var diagnostics = new List<DialectDiagnostic>();
        var source = new SyntaxDialectBindingSource(syntaxDocument);
        var expandedSource = _groupExpander.Expand(source, diagnostics);

        return DialectDefinitionSemanticBinder.BuildPlanCore(
            expandedSource,
            diagnostics,
            "S007",
            "Order rules contain a cycle involving modules",
            _directiveHandlerRegistry,
            "S002",
            "Order rule references module(s) not present in active module set");
    }
}
