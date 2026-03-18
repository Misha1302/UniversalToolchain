using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
/// Builds a validated and normalized DialectBuildPlan from framework-native compiled DSL output.
/// </summary>
public sealed class DialectCompiledDialectBuildPlanBuilder : IDialectCompiledDialectBuildPlanBuilder
{
    public DialectBuildPlan Build(DialectDefinitionSlice compiledDialect)
    {
        if (compiledDialect == null)
        {
            Thrower.ArgumentNull(nameof(compiledDialect));
        }

        var diagnostics = new List<DialectDiagnostic>();
        var definition = DialectDefinitionSemanticBinder.Bind(compiledDialect, diagnostics);
        return DialectDefinitionBuildPlanProjector.Project(
            definition,
            diagnostics,
            cycleCode: "S105",
            cycleMessagePrefix: "Order directives contain a cycle involving modules");
    }
}
