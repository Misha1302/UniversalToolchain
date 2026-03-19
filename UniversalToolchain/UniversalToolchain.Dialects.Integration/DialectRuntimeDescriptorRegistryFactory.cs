using System.Reflection;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Discovers runtime descriptor providers deterministically and builds immutable runtime descriptor registries.
/// </summary>
public static class DialectRuntimeDescriptorRegistryFactory
{
    public static DialectRuntimeDescriptorRegistry BuildFromProviders(IEnumerable<IDialectRuntimeDescriptorProvider> providers)
    {
        if (providers == null)
            Thrower.ArgumentNull(nameof(providers));

        var orderedProviders = providers
            .Select(x =>
            {
                if (x == null)
                    Thrower.Argument(nameof(providers), "Provider collection must not contain null entries.");

                return x;
            })
            .OrderBy(x => x.Order)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        var builder = new DialectRuntimeDescriptorRegistryBuilder();
        foreach (var provider in orderedProviders)
            provider.Register(builder);

        return builder.Build();
    }

    public static DialectRuntimeDescriptorRegistry BuildFromAssemblies(params Assembly[] assemblies)
    {
        if (assemblies == null)
            Thrower.ArgumentNull(nameof(assemblies));

        var providers = assemblies
            .Where(x => x != null)
            .SelectMany(x => x.GetTypes())
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(IDialectRuntimeDescriptorProvider).IsAssignableFrom(x))
            .Select(x => Activator.CreateInstance(x) as IDialectRuntimeDescriptorProvider ?? Thrower.InvalidOpEx<IDialectRuntimeDescriptorProvider>($"Could not create runtime descriptor provider '{x.FullName}'."))
            .ToList();

        return BuildFromProviders(providers);
    }
}