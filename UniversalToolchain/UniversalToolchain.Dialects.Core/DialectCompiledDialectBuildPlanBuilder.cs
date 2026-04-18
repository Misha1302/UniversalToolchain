using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
///     Builds a validated and normalized DialectBuildPlan from framework-native compiled DSL output.
/// </summary>
public sealed class DialectCompiledDialectBuildPlanBuilder : IDialectCompiledDialectBuildPlanBuilder
{
    public DialectBuildPlan Build(DialectDefinitionSlice compiledDialect)
    {
        compiledDialect = compiledDialect.ArgNotNull();

        var diagnostics = new List<DialectDiagnostic>();
        return DialectDefinitionSemanticBinder.BuildPlanCore(
            new CompiledDialectBindingSource(compiledDialect),
            diagnostics,
            "S105",
            "Order directives contain a cycle involving modules");
    }
}