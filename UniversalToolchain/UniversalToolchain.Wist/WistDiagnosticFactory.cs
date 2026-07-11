using System.Reflection;
using ExceptionsManager;
using CommonExceptions;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist;

internal static class WistDiagnosticFactory
{
    private const string DefaultSourceName = "<input>";

    public static IReadOnlyList<WistDiagnostic> FromException(
        Exception exception,
        string operationStage,
        string sourceName = DefaultSourceName)
    {
        exception = exception.ArgNotNull();

        if (exception is SsaRouteException ssaRouteException)
            return FromSsaRouteException(ssaRouteException, sourceName);

        var wistException = exception as WistException;
        var stage = wistException?.Stage ?? operationStage;
        var span = wistException?.Location is { } location
            ? new WistSourceSpan(
                string.IsNullOrWhiteSpace(location.File) ? sourceName : location.File!,
                Math.Max(0, location.Line),
                Math.Max(0, location.Column),
                Math.Max(0, location.Line),
                Math.Max(0, location.Column))
            : null;

        var hints = CreateHints(exception);

        return
        [
            new WistDiagnostic(
                ResolveCode(exception, stage, operationStage),
                WistDiagnosticSeverity.Error,
                stage,
                sourceName,
                exception.Message,
                span,
                hints)
        ];
    }

    private static IReadOnlyList<WistDiagnostic> FromSsaRouteException(
        SsaRouteException exception,
        string sourceName)
    {
        var routeDiagnostics = exception.Diagnostics.Count == 0
            ? [new SsaRouteDiagnostic("ssa.route.failed", exception.Message)]
            : exception.Diagnostics;

        return routeDiagnostics
            .Select(diagnostic => new WistDiagnostic(
                WistDiagnosticCodes.SsaRouteFailure,
                WistDiagnosticSeverity.Error,
                "Optimization",
                sourceName,
                $"{diagnostic.Code}: {diagnostic.Message}",
                Span: null,
                Hints:
                [
                    new WistDiagnosticHint(
                        "Use SSA Prefer for controlled fallback, or simplify the expression and inspect the attached optimization report before retrying Require/Debug.")
                ]))
            .ToArray();
    }

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
            "TypeSystem" or "Import" => WistDiagnosticCodes.TypeResolutionFailure,
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
