using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
///     Builds a validated and deterministic dialect build plan from parser output.
/// </summary>
public interface IDialectBuildPlanBuilder
{
    /// <summary>
    ///     Validates and converts parsed dialect syntax into a normalized build plan.
    /// </summary>
    /// <param name="syntaxDocument">Parsed dialect syntax document.</param>
    /// <returns>Normalized build plan with semantic diagnostics.</returns>
    DialectBuildPlan Build(DialectSyntaxDocument syntaxDocument);
}