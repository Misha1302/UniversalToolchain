using System.Reflection;
using CommonExceptions;
using ExceptionsManager;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist;

internal static class WistDiagnosticFactory
{
    private const string DefaultSourceName = "<input>";
    private const int SafeMessageLimit = 512;

    public static IReadOnlyList<WistDiagnostic> FromException(
        Exception exception,
        string operationStage,
        string sourceName = DefaultSourceName) =>
        FromException(exception, operationStage, WistDiagnosticExposure.Developer, sourceName);

    internal static IReadOnlyList<WistDiagnostic> FromException(
        Exception exception,
        string operationStage,
        WistDiagnosticExposure exposure,
        string sourceName = DefaultSourceName)
    {
        exception = exception.ArgNotNull();
        if (!Enum.IsDefined(exposure))
            throw new ArgumentOutOfRangeException(nameof(exposure), exposure, "Unknown Wist diagnostic exposure.");

        if (exception is SsaRouteException ssaRouteException)
            return FromSsaRouteException(ssaRouteException, exposure, sourceName);

        // Walk the chain: a binding failure is thrown as InvalidOperationException wrapping BindingException, so
        // reading only the outer exception made the typed stage invisible and the code fell back to the entry
        // point. WistFailureClassifier already walks the chain with Contains<TException>; the factory did not,
        // which is how one failure could be classified as UserInput and reported as a compilation failure.
        var wistException = FindWistException(exception);
        var stage = wistException?.Stage ?? operationStage;
        var span = wistException?.Location is { } location
            ? new WistSourceSpan(
                string.IsNullOrWhiteSpace(location.File) ? sourceName : location.File!,
                Math.Max(0, location.Line),
                Math.Max(0, location.Column),
                Math.Max(0, location.Line),
                Math.Max(0, location.Column))
            : null;

        return
        [
            new WistDiagnostic(
                // Code, stage and hints are resolved from the same typed exception. Resolving the stage from the
                // chain while asking the outer exception for its code is how a wrapped resource limit reported
                // UTC-WIST-VALIDATE-001 with no hint: the diagnostic contradicted itself.
                ResolveCode(wistException ?? exception, stage, operationStage),
                WistDiagnosticSeverity.Error,
                stage,
                sourceName,
                FormatMessage(exception.Message, exposure),
                span,
                CreateHints(wistException ?? exception))
        ];
    }

    /// <summary>
    ///     The <see cref="WistException" /> in the chain that carries the typed stage, if any.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Every wrapper in this code base is a plain framework type without a stage - for example
    ///         <c>InvalidOperationException</c> wrapping <see cref="CommonExceptions.BindingException" /> - so the only
    ///         <see cref="WistException" /> in a real chain is the one that explains the failure.
    ///     </para>
    ///     <para>
    ///         This is deliberately the first match rather than a "most specific" search. A rule that preferred the
    ///         innermost exception would be indistinguishable from this one on every chain the product builds, so
    ///         stating it would add precision that nothing measures.
    ///     </para>
    /// </remarks>
    private static WistException? FindWistException(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is WistException wist)
                return wist;
        }

        return null;
    }

    private static IReadOnlyList<WistDiagnostic> FromSsaRouteException(
        SsaRouteException exception,
        WistDiagnosticExposure exposure,
        string sourceName)
    {
        var routeDiagnostics = exception.Diagnostics.Count == 0
            ? [new SsaRouteDiagnostic("ssa.route.failed", exception.Message, "route")]
            : exception.Diagnostics;

        return routeDiagnostics
            .Select(diagnostic => new WistDiagnostic(
                WistDiagnosticCodes.SsaRouteFailure,
                WistDiagnosticSeverity.Error,
                ResolveSsaDiagnosticStage(diagnostic.Stage),
                sourceName,
                FormatMessage($"{diagnostic.Code}: {diagnostic.Message}", exposure),
                Span: null,
                Hints:
                [
                    new WistDiagnosticHint(
                        "Use SSA Prefer for controlled fallback, or simplify the expression and inspect the attached optimization report before retrying Require/Debug.")
                ]))
            .ToArray();
    }

    private static string FormatMessage(string message, WistDiagnosticExposure exposure)
    {
        if (exposure == WistDiagnosticExposure.Developer)
            return message;

        var singleLine = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return singleLine.Length <= SafeMessageLimit
            ? singleLine
            : string.Concat(singleLine.AsSpan(0, SafeMessageLimit), "…");
    }

    private static string ResolveSsaDiagnosticStage(string? stage) =>
        stage switch
        {
            "lowering" => "SSA Lowering",
            "optimization" => "SSA Optimization",
            "emission" => "SSA Emission",
            "route" => "SSA Route",
            _ => "Optimization"
        };

    private static string ResolveCode(Exception exception, string? stage, string operationStage)
    {
        if (exception is WistResourceLimitException resourceLimitException)
            return resourceLimitException.DiagnosticCode;
        if (exception is AmbiguousMatchException)
            return WistDiagnosticCodes.AmbiguousResolution;
        if (exception is TypeLoadException)
            return WistDiagnosticCodes.TypeResolutionFailure;

        return stage switch
        {
            "Lexer" => WistDiagnosticCodes.LexerFailure,
            "Parser" => WistDiagnosticCodes.ParserFailure,
            "Dialect" => WistDiagnosticCodes.DialectFailure,
            // Binding is name resolution - "Unknown identifier 'x'" - so it belongs with the resolution
            // codes. Without this branch the same BindingException produced VALIDATE-001 through
            // Validate and COMPILE-001 through TryCompile: the code described the entry point rather than
            // the failure, so a caller could not tell what went wrong, only which API they had used.
            "Binding" or "TypeSystem" or "Import" => WistDiagnosticCodes.TypeResolutionFailure,
            "Runtime" => WistDiagnosticCodes.ExecutionFailure,
            "Bytecode" or "InternalCompiler" => WistDiagnosticCodes.CompilationFailure,
            _ when string.Equals(operationStage, "Validation", StringComparison.Ordinal) => WistDiagnosticCodes.ValidationFailure,
            _ when string.Equals(operationStage, "Execution", StringComparison.Ordinal) => WistDiagnosticCodes.ExecutionFailure,
            _ when string.Equals(operationStage, "Compilation", StringComparison.Ordinal) => WistDiagnosticCodes.CompilationFailure,
            _ => WistDiagnosticCodes.UnexpectedFailure
        };
    }

    private static IReadOnlyList<WistDiagnosticHint> CreateHints(Exception exception)
    {
        if (exception is WistDialectFeatureException)
        {
            return
            [
                new WistDiagnosticHint(
                    "Use only constructs enabled by the selected dialect. Select a broader preset only for trusted input.")
            ];
        }

        if (exception is WistResourceLimitException)
        {
            return
            [
                new WistDiagnosticHint(
                    "Reduce the input or explicitly raise the host-owned limit after reviewing the trust and resource model.")
            ];
        }

        return Array.Empty<WistDiagnosticHint>();
    }
}
