using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
/// Validates parsed dialect syntax against deterministic semantic policy rules.
/// </summary>
public interface IDialectPolicyValidator
{
    /// <summary>
    /// Validates the provided parsed dialect syntax document.
    /// </summary>
    /// <param name="syntaxDocument">Parsed dialect syntax to validate.</param>
    /// <returns>Semantic validation diagnostics.</returns>
    DialectValidationResult Validate(DialectSyntaxDocument syntaxDocument);
}
