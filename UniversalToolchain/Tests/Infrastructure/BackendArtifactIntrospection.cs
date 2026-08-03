using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace Tests.Infrastructure;

internal static class BackendArtifactIntrospection
{
    public static object GetCompilationOutput(ICompiledArtifact artifact)
    {
        artifact = artifact.ArgNotNull();

        var output = artifact.GetType()
            .GetProperty("CompilationOutput", BindingFlags.Instance | BindingFlags.Public)?
            .GetValue(artifact);
        return output ?? Thrower.InvalidOpEx<object>(
            $"Artifact type '{artifact.GetType().FullName}' does not expose a non-null compilation output.");
    }

    public static DynamicMethod GetDynamicMethod(ICompiledArtifact artifact)
    {
        var output = GetCompilationOutput(artifact);
        return output.GetType()
                   .GetProperty("Method", BindingFlags.Instance | BindingFlags.Public)?
                   .GetValue(output) as DynamicMethod
               ?? Thrower.InvalidOpEx<DynamicMethod>(
                   $"Compilation output type '{output.GetType().FullName}' does not expose a DynamicMethod.");
    }

    public static string? GetOutputLoadContextName(ICompiledArtifact artifact)
    {
        var output = GetCompilationOutput(artifact);
        return AssemblyLoadContext.GetLoadContext(output.GetType().Assembly)?.Name;
    }
}
