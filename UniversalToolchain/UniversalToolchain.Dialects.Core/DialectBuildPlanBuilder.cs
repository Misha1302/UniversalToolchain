using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Core.Groups;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
///     Default semantic validator and deterministic build-plan builder.
/// </summary>
public sealed class DialectBuildPlanBuilder : IDialectBuildPlanBuilder
{
    private readonly DialectGroupExpander _groupExpander;

    public DialectBuildPlanBuilder()
        : this(new DialectGroupExpander(new EmptyDialectGroupCatalog()))
    {
    }

    public DialectBuildPlanBuilder(DialectGroupExpander groupExpander)
    {
        _groupExpander = groupExpander.ArgNotNull();
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
            "S002",
            "Order rule references module(s) not present in active module set");
    }
}