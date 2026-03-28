using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
///     Default semantic validator adapter backed by build-plan builder diagnostics.
/// </summary>
public sealed class DialectPolicyValidator
{
    private readonly IDialectBuildPlanBuilder _buildPlanBuilder;

    public DialectPolicyValidator(IDialectBuildPlanBuilder buildPlanBuilder)
    {
        if (buildPlanBuilder == null)
            Thrower.ArgumentNull(nameof(buildPlanBuilder));

        _buildPlanBuilder = buildPlanBuilder;
    }

    public DialectValidationResult Validate(DialectSyntaxDocument syntaxDocument)
    {
        if (syntaxDocument == null)
            Thrower.ArgumentNull(nameof(syntaxDocument));

        var plan = _buildPlanBuilder.Build(syntaxDocument);
        return plan.ValidationResult;
    }
}