namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
/// Parses dialect definition text into deterministic syntax data.
/// </summary>
public interface IDialectDefinitionParser
{
    /// <summary>
    /// Parses DSL source text into syntax document and diagnostics.
    /// </summary>
    /// <param name="sourceText">Dialect definition source text.</param>
    /// <returns>Parse result with document on success.</returns>
    DialectParseResult Parse(string sourceText);
}
