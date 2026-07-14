using System.Reflection;
using System.Runtime.Loader;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeAssemblyLoadStrategy : IRuntimeAssemblyLoadStrategy, IDisposable
{
    private readonly object _resolvingHandlerLock = new();
    private bool _resolvingHandlerRegistered;
    private readonly IRuntimeAssemblyLocator _locator;
    private bool _disposed;

    public DefaultRuntimeAssemblyLoadStrategy(IRuntimeAssemblyLocator locator)
    {
        locator = locator.ArgNotNull();

        _locator = locator;
    }

    public Assembly LoadAssembly(string assemblySimpleName)
    {
        lock (_resolvingHandlerLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (string.IsNullOrWhiteSpace(assemblySimpleName))
                Thrower.Argument(nameof(assemblySimpleName), "Assembly simple name must not be empty.");

            var alreadyLoaded = TryGetAlreadyLoadedAssembly(assemblySimpleName);
            if (alreadyLoaded != null)
                return alreadyLoaded;

            var loadedByName = TryLoadBySimpleName(assemblySimpleName);
            if (loadedByName != null)
                return loadedByName;

            // Resolve the root path exactly once before installing the process-wide
            // dependency callback. The callback is needed while LoadFromAssemblyPath
            // loads transitive dependencies, but using it for the root probe would
            // duplicate locator calls and blur root/dependency ownership.
            var absolutePath = ResolveRequiredAssemblyPath(assemblySimpleName);
            EnsureResolvingHandlerRegistered();
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(absolutePath);
        }
    }

    public void Dispose()
    {
        lock (_resolvingHandlerLock)
        {
            if (_disposed)
                return;

            if (_resolvingHandlerRegistered)
            {
                AssemblyLoadContext.Default.Resolving -= ResolveFromConfiguredRuntimeRoots;
                _resolvingHandlerRegistered = false;
            }

            _disposed = true;
        }
    }

    private void EnsureResolvingHandlerRegistered()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_resolvingHandlerRegistered)
            return;

        lock (_resolvingHandlerLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_resolvingHandlerRegistered)
                return;

            AssemblyLoadContext.Default.Resolving += ResolveFromConfiguredRuntimeRoots;
            _resolvingHandlerRegistered = true;
        }
    }

    private Assembly? ResolveFromConfiguredRuntimeRoots(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        if (Volatile.Read(ref _disposed))
            return null;

        var simpleName = assemblyName.Name;
        if (string.IsNullOrWhiteSpace(simpleName))
            return null;

        var alreadyLoaded = TryGetAlreadyLoadedAssembly(simpleName);
        if (alreadyLoaded != null)
            return alreadyLoaded;

        if (!_locator.TryResolveAssemblyPath(simpleName, out var absolutePath) || string.IsNullOrWhiteSpace(absolutePath))
            return null;

        if (!Path.IsPathRooted(absolutePath))
            return null;

        try
        {
            return context.LoadFromAssemblyPath(absolutePath);
        }
        catch (FileLoadException)
        {
            return TryGetAlreadyLoadedAssembly(simpleName);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    private static Assembly? TryGetAlreadyLoadedAssembly(string assemblySimpleName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(x => string.Equals(x.GetName().Name, assemblySimpleName, StringComparison.Ordinal));
    }

    private static Assembly? TryLoadBySimpleName(string assemblySimpleName)
    {
        try
        {
            return Assembly.Load(new AssemblyName(assemblySimpleName));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    private string ResolveRequiredAssemblyPath(string assemblySimpleName)
    {
        if (!_locator.TryResolveAssemblyPath(assemblySimpleName, out var absolutePath) || string.IsNullOrWhiteSpace(absolutePath))
            Thrower.FileNotFound($"Assembly '{assemblySimpleName}' was not found in configured runtime assembly locator search roots.");

        if (!Path.IsPathRooted(absolutePath))
            Thrower.Argument(nameof(absolutePath), $"Assembly locator returned non-absolute path '{absolutePath}'.");

        return absolutePath;
    }
}
