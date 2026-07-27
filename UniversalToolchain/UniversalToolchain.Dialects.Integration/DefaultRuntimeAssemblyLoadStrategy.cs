using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Loads the runtime closure only from the configured locator roots.
/// Every strategy owns an isolated load context, while assemblies already loaded
/// from the exact same file are shared through the default context. A same-name
/// process preload from any other path is never treated as authoritative.
/// </summary>
public sealed class DefaultRuntimeAssemblyLoadStrategy : IRuntimeAssemblyLoadStrategy, IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<Assembly>> _assemblyCache = new(StringComparer.Ordinal);
    private readonly ConfiguredRuntimeAssemblyLoadContext _loadContext;
    private bool _disposed;

    public DefaultRuntimeAssemblyLoadStrategy(IRuntimeAssemblyLocator locator)
    {
        locator = locator.ArgNotNull();
        _loadContext = new ConfiguredRuntimeAssemblyLoadContext(locator);
    }

    public Assembly LoadAssembly(string assemblySimpleName)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            Thrower.Argument(nameof(assemblySimpleName), "Assembly simple name must not be empty.");

        var normalizedName = assemblySimpleName.Trim();
        var lazy = _assemblyCache.GetOrAdd(
            normalizedName,
            static (name, context) => new Lazy<Assembly>(
                () => context.LoadRootAssembly(name),
                LazyThreadSafetyMode.ExecutionAndPublication),
            _loadContext);
        return lazy.Value;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _assemblyCache.Clear();
        _loadContext.Unload();
    }

    private sealed class ConfiguredRuntimeAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly IRuntimeAssemblyLocator _locator;

        public ConfiguredRuntimeAssemblyLoadContext(IRuntimeAssemblyLocator locator)
            : base("UniversalToolchain.Runtime.Isolated", isCollectible: true)
        {
            _locator = locator;
        }

        public Assembly LoadRootAssembly(string assemblySimpleName)
        {
            var absolutePath = ResolveRequiredPath(assemblySimpleName);
            var fileIdentity = ReadAndValidateIdentity(absolutePath, assemblySimpleName, requestedIdentity: null);

            var exactDefaultAssembly = FindDefaultAssemblyLoadedFrom(absolutePath, fileIdentity);
            if (exactDefaultAssembly != null)
                return exactDefaultAssembly;

            return LoadFromAssemblyPath(absolutePath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var simpleName = assemblyName.Name;
            if (string.IsNullOrWhiteSpace(simpleName))
                return null;

            if (!_locator.TryResolveAssemblyPath(simpleName, out var path) || string.IsNullOrWhiteSpace(path))
                return null;
            if (!Path.IsPathRooted(path))
                Thrower.Argument(nameof(path), $"Assembly locator returned non-absolute path '{path}'.");

            var absolutePath = Path.GetFullPath(path);
            var fileIdentity = ReadAndValidateIdentity(absolutePath, simpleName, assemblyName);
            var exactDefaultAssembly = FindDefaultAssemblyLoadedFrom(absolutePath, fileIdentity);
            if (exactDefaultAssembly != null)
                return exactDefaultAssembly;

            // A runtime implementation can reference a contract type whose owning
            // assembly is already shared with the host. Loading that referenced
            // contract into this isolated context would create a split type identity
            // (for example, a constructor parameter from a second copy of the same
            // contract assembly). Share only dependencies proven to be referenced by
            // an exact configured assembly already loaded in the default context.
            var trustedSharedDependency = TryLoadTrustedDefaultDependency(absolutePath, fileIdentity);
            if (trustedSharedDependency != null)
                return trustedSharedDependency;

            return LoadFromAssemblyPath(absolutePath);
        }

        private Assembly? TryLoadTrustedDefaultDependency(string absolutePath, AssemblyName fileIdentity)
        {
            foreach (var defaultAssembly in Default.Assemblies
                         .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                         .OrderBy(static assembly => assembly.FullName, StringComparer.Ordinal))
            {
                if (!IsExactConfiguredDefaultAssembly(defaultAssembly))
                    continue;

                if (!defaultAssembly.GetReferencedAssemblies()
                        .Any(reference => ReferenceMatchesExactly(reference, fileIdentity)))
                {
                    continue;
                }

                var conflictingAssembly = Default.Assemblies.FirstOrDefault(assembly =>
                    !assembly.IsDynamic &&
                    string.Equals(assembly.GetName().Name, fileIdentity.Name, StringComparison.Ordinal));
                if (conflictingAssembly != null)
                {
                    if (ReferenceMatchesExactly(fileIdentity, conflictingAssembly.GetName()) &&
                        !string.IsNullOrWhiteSpace(conflictingAssembly.Location) &&
                        string.Equals(
                            Path.GetFullPath(conflictingAssembly.Location),
                            Path.GetFullPath(absolutePath),
                            StringComparison.Ordinal))
                    {
                        return conflictingAssembly;
                    }

                    throw new InvalidOperationException(
                        $"Configured runtime dependency '{fileIdentity.FullName}' at '{absolutePath}' conflicts with " +
                        $"default-context assembly '{conflictingAssembly.FullName}' at '{conflictingAssembly.Location}'.");
                }

                var loaded = Default.LoadFromAssemblyPath(absolutePath);
                if (!ReferenceMatchesExactly(fileIdentity, loaded.GetName()) ||
                    loaded.IsDynamic ||
                    string.IsNullOrWhiteSpace(loaded.Location) ||
                    !string.Equals(
                        Path.GetFullPath(loaded.Location),
                        Path.GetFullPath(absolutePath),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Default-context load of configured runtime dependency '{fileIdentity.FullName}' did not preserve exact path and identity.");
                }

                return loaded;
            }

            return null;
        }

        private bool IsExactConfiguredDefaultAssembly(Assembly assembly)
        {
            var simpleName = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(simpleName) ||
                !_locator.TryResolveAssemblyPath(simpleName, out var configuredPath) ||
                string.IsNullOrWhiteSpace(configuredPath) ||
                !Path.IsPathRooted(configuredPath))
            {
                return false;
            }

            var absoluteConfiguredPath = Path.GetFullPath(configuredPath);
            if (!string.Equals(
                    Path.GetFullPath(assembly.Location),
                    absoluteConfiguredPath,
                    StringComparison.Ordinal))
            {
                return false;
            }

            var configuredIdentity = ReadAndValidateIdentity(
                absoluteConfiguredPath,
                simpleName,
                requestedIdentity: null);
            return ReferenceMatchesExactly(configuredIdentity, assembly.GetName());
        }

        private string ResolveRequiredPath(string assemblySimpleName)
        {
            if (!_locator.TryResolveAssemblyPath(assemblySimpleName, out var path) || string.IsNullOrWhiteSpace(path))
            {
                throw new FileNotFoundException(
                    $"Assembly '{assemblySimpleName}' was not found in configured runtime assembly locator search roots.");
            }

            if (!Path.IsPathRooted(path))
                Thrower.Argument(nameof(path), $"Assembly locator returned non-absolute path '{path}'.");

            return Path.GetFullPath(path);
        }

        private static AssemblyName ReadAndValidateIdentity(
            string absolutePath,
            string expectedSimpleName,
            AssemblyName? requestedIdentity)
        {
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException($"Configured runtime assembly '{absolutePath}' does not exist.", absolutePath);

            AssemblyName fileIdentity;
            try
            {
                fileIdentity = AssemblyName.GetAssemblyName(absolutePath);
            }
            catch (BadImageFormatException exception)
            {
                throw new InvalidOperationException(
                    $"Configured runtime assembly '{absolutePath}' is not a valid managed assembly.",
                    exception);
            }

            if (!string.Equals(fileIdentity.Name, expectedSimpleName, StringComparison.Ordinal))
            {
                return Thrower.InvalidOpEx<AssemblyName>(
                    $"Configured runtime path '{absolutePath}' contains assembly '{fileIdentity.Name}', not requested assembly '{expectedSimpleName}'.");
            }

            if (requestedIdentity != null && !ReferenceMatchesExactly(requestedIdentity, fileIdentity))
            {
                return Thrower.InvalidOpEx<AssemblyName>(
                    $"Configured runtime assembly '{absolutePath}' has identity '{fileIdentity.FullName}', which does not satisfy requested identity '{requestedIdentity.FullName}'.");
            }

            return fileIdentity;
        }

        private static Assembly? FindDefaultAssemblyLoadedFrom(string absolutePath, AssemblyName expectedIdentity)
        {
            var normalizedPath = Path.GetFullPath(absolutePath);
            foreach (var assembly in Default.Assemblies)
            {
                var identity = assembly.GetName();
                if (!ReferenceMatchesExactly(expectedIdentity, identity))
                    continue;

                if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))
                    continue;

                if (string.Equals(Path.GetFullPath(assembly.Location), normalizedPath, StringComparison.Ordinal))
                    return assembly;
            }

            return null;
        }

        private static bool ReferenceMatchesExactly(AssemblyName expected, AssemblyName actual)
        {
            if (!string.Equals(expected.Name, actual.Name, StringComparison.Ordinal) ||
                expected.Version != actual.Version ||
                !string.Equals(NormalizeCulture(expected.CultureName), NormalizeCulture(actual.CultureName), StringComparison.Ordinal))
            {
                return false;
            }

            return expected.GetPublicKeyToken().AsSpan().SequenceEqual(actual.GetPublicKeyToken());
        }

        private static string NormalizeCulture(string? value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value;
    }
}
