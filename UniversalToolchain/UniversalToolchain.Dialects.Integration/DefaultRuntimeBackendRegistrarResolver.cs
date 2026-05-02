using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Resolves backend runtime registrars declared by exact backend activation metadata.
/// </summary>
public sealed class DefaultRuntimeBackendRegistrarResolver : IRuntimeBackendRegistrarResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRuntimeAssemblyTypeLoader _typeLoader;

    public DefaultRuntimeBackendRegistrarResolver(
        IRuntimeAssemblyTypeLoader typeLoader,
        IServiceProvider serviceProvider)
    {
        _typeLoader = typeLoader.ArgNotNull();
        _serviceProvider = serviceProvider.ArgNotNull();
    }

    public IDialectBackendRuntimeRegistrar Resolve(RuntimeComponentManifestEntry backendEntry)
    {
        backendEntry = backendEntry.ArgNotNull();

        if (backendEntry.Kind != RuntimeComponentKind.Backend)
            Thrower.InvalidOpEx(
                $"Runtime component '{backendEntry.CanonicalAlias}' has kind '{RuntimeComponentKindCodec.Format(backendEntry.Kind)}', but '{RuntimeComponentKindCodec.Format(RuntimeComponentKind.Backend)}' was expected.");

        var registrarTypeReference = backendEntry.Activation?.RegistrarType;
        if (registrarTypeReference == null)
            Thrower.InvalidOpEx(
                $"Runtime backend manifest entry '{backendEntry.CanonicalAlias}' does not declare registrarTypeFullName activation metadata.");

        var registrarAssemblySimpleName = ResolveAssemblySimpleName(registrarTypeReference.AssemblySimpleName, backendEntry.AssemblySimpleName);
        var registrarType = _typeLoader.LoadType(registrarAssemblySimpleName, registrarTypeReference.TypeFullName);
        if (!typeof(IDialectBackendRuntimeRegistrar).IsAssignableFrom(registrarType))
            Thrower.InvalidOpEx(
                $"Runtime backend registrar type '{DisplayName(registrarType)}' for backend '{backendEntry.CanonicalAlias}' does not implement IDialectBackendRuntimeRegistrar.");

        var registrar = (IDialectBackendRuntimeRegistrar)ActivatorUtilities.CreateInstance(_serviceProvider, registrarType);
        var expectedBackendId = new DialectBackendId(backendEntry.CanonicalAlias);
        if (registrar.BackendId != expectedBackendId)
            Thrower.InvalidOpEx(
                $"Runtime backend registrar '{DisplayName(registrarType)}' declares backend id '{registrar.BackendId.Value}', but manifest backend alias '{backendEntry.CanonicalAlias}' was selected.");

        return registrar;
    }

    private static string DisplayName(Type type) => type.FullName ?? type.Name;

    private static string ResolveAssemblySimpleName(string assemblySimpleName, string fallbackAssemblySimpleName) =>
        string.Equals(assemblySimpleName, RuntimeAssemblyIdentity.UnspecifiedAssemblySimpleName, StringComparison.Ordinal)
            ? fallbackAssemblySimpleName
            : assemblySimpleName;
}