using System.Reflection;
using CommonExceptions;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist;

internal static class WistFailureClassifier
{
    public static WistFailureKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            WistResourceLimitException => WistFailureKind.Policy,
            WistUserInputException => WistFailureKind.UserInput,
            WistDialectFeatureException => WistFailureKind.UserInput,
            LexerException or ParserException or TypeSystemException or ImportException => WistFailureKind.UserInput,
            AmbiguousMatchException or TypeLoadException => WistFailureKind.UserInput,
            SsaRouteException => WistFailureKind.Unsupported,
            FileLoadException or FileNotFoundException or IOException or UnauthorizedAccessException =>
                WistFailureKind.Infrastructure,
            InternalCompilerException or BytecodeGenerationException or RuntimeExecutionException => WistFailureKind.Internal,
            _ => WistFailureKind.Internal
        };
    }

    public static bool IsStructuredResultFailure(WistFailureKind kind) =>
        kind is WistFailureKind.UserInput or WistFailureKind.Policy or WistFailureKind.Unsupported;
}
