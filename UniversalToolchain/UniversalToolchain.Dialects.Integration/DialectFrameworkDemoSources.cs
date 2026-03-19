using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Provides minimal demo sources that exercise the framework-native dialect pipeline.
/// </summary>
public static class DialectFrameworkDemoSources
{
    public static string GetSource(DialectFrameworkDemoScenario scenario)
    {
        return scenario switch
        {
            DialectFrameworkDemoScenario.Valid =>
                """
                dialect FrameworkNativeDemo
                use Arithmetic
                use Variables
                before Arithmetic -> Variables
                backend interpreter enable
                allow intrinsic "add_i32" for any
                enable optimizer LocalVariablesOptimization for interpreter
                security trusted
                """,
            DialectFrameworkDemoScenario.InvalidSyntax =>
                """
                dialect FrameworkNativeDemo
                use Arithmetic
                backend interpreter ???
                """,
            DialectFrameworkDemoScenario.SemanticConflict =>
                """
                dialect FrameworkNativeDemo
                use Arithmetic
                exclude Arithmetic
                """,
            DialectFrameworkDemoScenario.UnresolvedModule =>
                """
                dialect FrameworkNativeDemo
                use MissingModule
                backend interpreter enable
                security trusted
                """,
            _ => ThrowUnsupportedScenario(scenario)
        };
    }

    private static string ThrowUnsupportedScenario(DialectFrameworkDemoScenario scenario)
    {
        Thrower.Argument(nameof(scenario), $"Unsupported demo scenario '{scenario}'.");
        return string.Empty;
    }
}