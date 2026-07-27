using System.Reflection.Emit;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Infrastructure;

internal static class ParityBackendExecutionAdapter
{
    public static BackendArtifactSnapshot CompileSnapshot(
        WistDialectExecutionHost host,
        string backendName,
        string code,
        OrderedDictionary<string, Type> declared)
    {
        var artifact = CompileArtifact(host, backendName, code, declared);
        return new BackendArtifactSnapshot(
            artifact.DeclaredBindings.Select(static binding => binding.Name).ToArray(),
            new Dictionary<string, int>(artifact.SlotsByName, StringComparer.Ordinal));
    }

    public static object RunCompiled(
        WistDialectExecutionHost host,
        string backendName,
        string code,
        OrderedDictionary<string, Type> declared,
        IReadOnlyList<KeyValuePair<string, object>> arguments)
    {
        var artifact = CompileArtifact(host, backendName, code, declared);
        var session = artifact.CreateSession();

        foreach (var argument in arguments)
            session.SetArgument(argument.Key, argument.Value);

        return session.Run() ?? Thrower.InvalidOpEx<object>($"Backend '{backendName}' returned null result.");
    }

    private static ICompiledArtifact CompileArtifact(
        WistDialectExecutionHost host,
        string backendName,
        string code,
        OrderedDictionary<string, Type> declared)
    {
        return backendName switch
        {
            "cil" => host.GetBackendSpecificArtifactCompiler<CilCompilationOutput>(backendName).Compile(code, declared),
            "interpreter" => host.GetBackendSpecificArtifactCompiler<IAbstractIR>(backendName).Compile(code, declared),
            _ => Thrower.InvalidOpEx<ICompiledArtifact>($"Unsupported backend '{backendName}'.")
        };
    }

    internal sealed record BackendArtifactSnapshot(
        IReadOnlyList<string> DeclaredBindingNames,
        IReadOnlyDictionary<string, int> SlotsByName);
}
