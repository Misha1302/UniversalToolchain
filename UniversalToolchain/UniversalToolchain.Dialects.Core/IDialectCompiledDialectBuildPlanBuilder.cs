namespace UniversalToolchain.Dialects.Core;

/// <summary>
///     Builds a validated and deterministic dialect build plan from framework-native compiled DSL output.
/// </summary>
public interface IDialectCompiledDialectBuildPlanBuilder
{
    /// <summary>
    ///     Validates and normalizes compiled dialect directives into a build plan.
    /// </summary>
    /// <param name="compiledDialect">Compiled dialect definition slice.</param>
    /// <returns>Normalized build plan with semantic diagnostics.</returns>
    DialectBuildPlan Build(DialectDefinitionSlice compiledDialect);
}