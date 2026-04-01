using System.Reflection;
using System.Runtime.Loader;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeAssemblyLoadStrategy : IRuntimeAssemblyLoadStrategy
{
    private readonly IRuntimeAssemblyLocator _locator;

    public DefaultRuntimeAssemblyLoadStrategy(IRuntimeAssemblyLocator locator)
    {
        if (locator == null)
            Thrower.ArgumentNull(nameof(locator));

        _locator = locator;
    }

    public Assembly LoadAssembly(string assemblySimpleName)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            Thrower.Argument(nameof(assemblySimpleName), "Assembly simple name must not be empty.");

        return TryGetAlreadyLoadedAssembly(assemblySimpleName)
               ?? TryLoadBySimpleName(assemblySimpleName)
               ?? LoadAssemblyFromResolvedPath(assemblySimpleName);
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

    private Assembly LoadAssemblyFromResolvedPath(string assemblySimpleName)
    {
        if (!_locator.TryResolveAssemblyPath(assemblySimpleName, out var absolutePath) || string.IsNullOrWhiteSpace(absolutePath))
            Thrower.FileNotFound($"Assembly '{assemblySimpleName}' was not found in configured runtime assembly locator search roots.");

        if (!Path.IsPathRooted(absolutePath))
            Thrower.Argument(nameof(absolutePath), $"Assembly locator returned non-absolute path '{absolutePath}'.");

        return AssemblyLoadContext.Default.LoadFromAssemblyPath(absolutePath);
    }
}