using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Provides backend-aware access to a dialect-configured Wist runtime.
/// </summary>
public sealed class WistDialectExecutionHost : IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    public WistDialectExecutionHost(IServiceProvider serviceProvider, WistDialectExecutionConfiguration configuration)
    {
        if (serviceProvider == null)
            Thrower.ArgumentNull(nameof(serviceProvider));

        if (configuration == null)
            Thrower.ArgumentNull(nameof(configuration));

        _serviceProvider = serviceProvider;
        Configuration = configuration;
    }

    public WistDialectExecutionConfiguration Configuration { get; }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    public ICoreRunnable GetCore(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            Thrower.Argument(nameof(mode), "Execution mode must not be empty.");

        if (!Configuration.TryResolveKnownBackendId(mode, out var backendId))
        {
            var supportedModes = string.Join(", ", Configuration.EnabledBackends.SelectMany(x => x.AllNames).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal));
            Thrower.InvalidOpEx($"Unknown execution mode '{mode}'. Supported modes: {supportedModes}.");
        }

        if (!Configuration.TryGetEnabledBackend(backendId, out var backendConfiguration))
            Thrower.InvalidOpEx($"Dialect '{Configuration.DialectName}' does not enable the '{mode}' backend.");

        var runtime = _serviceProvider.GetServices<WistDialectBackendRuntime>()
            .FirstOrDefault(x => x.Descriptor.BackendId == backendConfiguration.BackendDescriptor.BackendId);
        if (runtime == null)
            Thrower.InvalidOpEx<ICoreRunnable>($"Backend core '{backendConfiguration.BackendDescriptor.CanonicalId}' was not registered.");

        return runtime.Core;
    }

    public object? Run(string code, string mode) => GetCore(mode).Run(code);
}