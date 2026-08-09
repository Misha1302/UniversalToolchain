namespace Tests.Infrastructure;

internal static class ParityBackendExecutionAdapter
{
    public static BackendArtifactSnapshot CompileSnapshot(
        CanonicalWistTestHost host,
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
        CanonicalWistTestHost host,
        string backendName,
        string code,
        OrderedDictionary<string, Type> declared,
        IReadOnlyList<KeyValuePair<string, object>> arguments)
    {
        var artifact = CompileArtifact(host, backendName, code, declared);
        var runtimeArguments = arguments.ToDictionary(
            static argument => argument.Key,
            static argument => (object?)argument.Value,
            StringComparer.Ordinal);
        return host.Run(artifact, runtimeArguments)
               ?? Thrower.InvalidOpEx<object>($"Backend '{backendName}' returned null result.");
    }

    private static CanonicalWistBuiltProgram CompileArtifact(
        CanonicalWistTestHost host,
        string backendName,
        string code,
        OrderedDictionary<string, Type> declared)
    {
        if (backendName is not ("cil" or "interpreter"))
            return Thrower.InvalidOpEx<CanonicalWistBuiltProgram>($"Unsupported backend '{backendName}'.");

        return host.Compile(code, declared, backendName);
    }

    internal sealed record BackendArtifactSnapshot(
        IReadOnlyList<string> DeclaredBindingNames,
        IReadOnlyDictionary<string, int> SlotsByName);
}
