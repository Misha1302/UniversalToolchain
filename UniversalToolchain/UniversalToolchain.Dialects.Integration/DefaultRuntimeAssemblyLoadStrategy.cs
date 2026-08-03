using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Loads runtime implementations from configured roots into one collectible context.
/// Default-context identity is used only when the host explicitly registered and validated
/// the configured assembly through <see cref="IRuntimeSharedAssemblyResolver"/>.
/// </summary>
public sealed class DefaultRuntimeAssemblyLoadStrategy : IRuntimeAssemblyLoadStrategy, IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<Assembly>> _assemblyCache = new(StringComparer.Ordinal);
    private readonly ConfiguredRuntimeAssemblyLoadContext _loadContext;
    private int _disposeState;

    public DefaultRuntimeAssemblyLoadStrategy(IRuntimeAssemblyLocator locator)
        : this(locator, new DefaultRuntimeSharedAssemblyResolver([]))
    {
    }

    public DefaultRuntimeAssemblyLoadStrategy(
        IRuntimeAssemblyLocator locator,
        IRuntimeSharedAssemblyResolver sharedAssemblyResolver)
    {
        locator = locator.ArgNotNull();
        sharedAssemblyResolver = sharedAssemblyResolver.ArgNotNull();
        _loadContext = new ConfiguredRuntimeAssemblyLoadContext(locator, sharedAssemblyResolver);
    }

    public Assembly LoadAssembly(string assemblySimpleName)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
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
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        _assemblyCache.Clear();
        _loadContext.Unload();
    }


    private static class RuntimePlatformAssemblyPolicy
    {
        private static readonly ImmutableHashSet<RuntimeAssemblyIdentity> RuntimeOwnedIdentities =
            BuildRuntimeOwnedIdentitySnapshot();

        public static bool IsRuntimeOwnedPlatformAssembly(AssemblyName requestedIdentity) =>
            RuntimeOwnedIdentities.Contains(RuntimeAssemblyIdentity.FromAssemblyName(requestedIdentity));

        private static ImmutableHashSet<RuntimeAssemblyIdentity> BuildRuntimeOwnedIdentitySnapshot()
        {
            var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
                return ImmutableHashSet<RuntimeAssemblyIdentity>.Empty;

            var runtimeDirectory = Path.GetFullPath(RuntimeEnvironment.GetRuntimeDirectory());
            var builder = ImmutableHashSet.CreateBuilder<RuntimeAssemblyIdentity>();
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var absolutePath = Path.GetFullPath(path);
                    if (!IsWithinDirectory(absolutePath, runtimeDirectory))
                        continue;

                    builder.Add(RuntimeAssemblyIdentity.FromAssemblyName(AssemblyName.GetAssemblyName(absolutePath)));
                }
                catch (Exception exception) when (
                    exception is FileNotFoundException or
                    BadImageFormatException or
                    FileLoadException or
                    UnauthorizedAccessException)
                {
                    // Ignore malformed runtime entries. A non-snapshotted identity remains fail-closed.
                }
            }

            return builder.ToImmutable();
        }

        private static bool IsWithinDirectory(string path, string directory)
        {
            var relative = Path.GetRelativePath(directory, path);
            return !Path.IsPathRooted(relative) &&
                   !relative.Equals("..", StringComparison.Ordinal) &&
                   !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                   !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
        }
    }

    private sealed class ConfiguredRuntimeAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly IRuntimeAssemblyLocator _locator;
        private readonly IRuntimeSharedAssemblyResolver _sharedAssemblyResolver;

        public ConfiguredRuntimeAssemblyLoadContext(
            IRuntimeAssemblyLocator locator,
            IRuntimeSharedAssemblyResolver sharedAssemblyResolver)
            : base("UniversalToolchain.Runtime.Isolated", isCollectible: true)
        {
            _locator = locator;
            _sharedAssemblyResolver = sharedAssemblyResolver;
        }

        public Assembly LoadRootAssembly(string assemblySimpleName)
        {
            var absolutePath = ResolveRequiredPath(assemblySimpleName);
            var fileIdentity = ReadAndValidateIdentity(absolutePath, assemblySimpleName, requestedIdentity: null);
            var shared = _sharedAssemblyResolver.Resolve(fileIdentity, absolutePath);
            return shared.Kind == RuntimeSharedAssemblyResolutionKind.Shared
                ? shared.Assembly!
                : LoadFromAssemblyPath(absolutePath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var simpleName = assemblyName.Name;
            if (string.IsNullOrWhiteSpace(simpleName))
                return null;

            if (!_locator.TryResolveAssemblyPath(simpleName, out var path) || string.IsNullOrWhiteSpace(path))
            {
                if (RuntimePlatformAssemblyPolicy.IsRuntimeOwnedPlatformAssembly(assemblyName))
                    return null;

                throw new FileNotFoundException(
                    $"Runtime dependency '{assemblyName.FullName}' was not found in configured runtime assembly locator search roots. " +
                    "Fallback to an assembly from the default context is forbidden unless the host explicitly registered it as shared.");
            }
            if (!Path.IsPathRooted(path))
                Thrower.Argument(nameof(path), $"Assembly locator returned non-absolute path '{path}'.");

            var absolutePath = Path.GetFullPath(path);
            ReadAndValidateIdentity(absolutePath, simpleName, assemblyName);
            var shared = _sharedAssemblyResolver.Resolve(assemblyName, absolutePath);
            return shared.Kind == RuntimeSharedAssemblyResolutionKind.Shared
                ? shared.Assembly!
                : LoadFromAssemblyPath(absolutePath);
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

            if (requestedIdentity != null &&
                RuntimeAssemblyIdentity.FromAssemblyName(requestedIdentity) != RuntimeAssemblyIdentity.FromAssemblyName(fileIdentity))
            {
                return Thrower.InvalidOpEx<AssemblyName>(
                    $"Configured runtime assembly '{absolutePath}' has identity '{fileIdentity.FullName}', which does not satisfy requested identity '{requestedIdentity.FullName}'.");
            }

            return fileIdentity;
        }
    }
}
