using System.Collections.Specialized;
using UniversalToolchain.Dialects.Wist;
using System.Reflection.Emit;
using ExceptionsManager;

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

    private static ICompiledArtifact<object> CompileArtifact(
        WistDialectExecutionHost host,
        string backendName,
        string code,
        OrderedDictionary<string, Type> declared)
    {
        return backendName switch
        {
            "compiler" => Wrap(host.GetArtifactCompiler<DynamicMethod>(backendName).Compile(code, declared)),
            "interpreter" => Wrap(host.GetArtifactCompiler<IAbstractIR>(backendName).Compile(code, declared)),
            _ => Thrower.InvalidOpEx<ICompiledArtifact<object>>($"Unsupported backend '{backendName}'.")
        };
    }

    private static ICompiledArtifact<object> Wrap<TCompilationOutput>(ICompiledArtifact<TCompilationOutput> artifact)
        => new ArtifactAdapter<TCompilationOutput>(artifact);

    internal sealed record BackendArtifactSnapshot(
        IReadOnlyList<string> DeclaredBindingNames,
        IReadOnlyDictionary<string, int> SlotsByName);

    private sealed class ArtifactAdapter<TCompilationOutput>(ICompiledArtifact<TCompilationOutput> inner) : ICompiledArtifact<object>
    {
        public string SourceText => inner.SourceText;

        public IReadOnlyList<ExternalBinding> DeclaredBindings => inner.DeclaredBindings;

        public IReadOnlyDictionary<string, int> SlotsByName => inner.SlotsByName;

        public object CompilationOutput => inner.CompilationOutput!;

        public ICompiledArtifactSession CreateSession() => inner.CreateSession();
    }
}
