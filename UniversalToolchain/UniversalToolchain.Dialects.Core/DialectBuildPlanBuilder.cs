using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
///     Default semantic validator and deterministic build-plan builder.
/// </summary>
public sealed class DialectBuildPlanBuilder : IDialectBuildPlanBuilder
{
    public DialectBuildPlan Build(DialectSyntaxDocument syntaxDocument)
    {
        syntaxDocument = syntaxDocument.ArgNotNull();

        var diagnostics = new List<DialectDiagnostic>();
        return DialectDefinitionSemanticBinder.BuildPlanCore(
            new SyntaxDialectBindingSource(syntaxDocument),
            diagnostics,
            "S007",
            "Order rules contain a cycle involving modules",
            "S002",
            "Order rule references module(s) not present in active module set");
    }
}
