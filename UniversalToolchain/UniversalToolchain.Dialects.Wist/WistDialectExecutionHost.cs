using BasicCore.Contracts;
using BasicCore.Core;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Emit;
using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Provides backend-aware access to a dialect-configured Wist runtime.
/// </summary>
public sealed class WistDialectExecutionHost : IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    public WistDialectExecutionHost(IServiceProvider serviceProvider, WistDialectExecutionConfiguration configuration)
    {
        if (serviceProvider == null)
        {
            Thrower.ArgumentNull(nameof(serviceProvider));
        }

        if (configuration == null)
        {
            Thrower.ArgumentNull(nameof(configuration));
        }

        _serviceProvider = serviceProvider;
        Configuration = configuration;
    }

    public WistDialectExecutionConfiguration Configuration { get; }

    public ICoreRunnable GetCore(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            Thrower.Argument(nameof(mode), "Execution mode must not be empty.");
        }

        var target = ParseMode(mode);
        if (!Configuration.EnabledBackends.Contains(target))
        {
            Thrower.InvalidOpEx($"Dialect '{Configuration.DialectName}' does not enable the '{mode}' backend.");
        }

        var runnables = _serviceProvider.GetServices(typeof(ICoreRunnable)).Cast<ICoreRunnable>().ToList();
        return target switch
        {
            DialectBackendTarget.Cil => runnables.FirstOrDefault(x => IsCoreForCompilationType(x, typeof(DynamicMethod)))
                                        ?? Thrower.InvalidOpEx<ICoreRunnable>("Compiler backend core was not registered."),
            DialectBackendTarget.Interpreter => runnables.FirstOrDefault(x => IsCoreForCompilationType(x, typeof(IAbstractIR)))
                                                ?? Thrower.InvalidOpEx<ICoreRunnable>("Interpreter backend core was not registered."),
            _ => Thrower.InvalidOpEx<ICoreRunnable>($"Unsupported backend '{target}'.")
        };
    }

    public object? Run(string code, string mode)
    {
        return GetCore(mode).Run(code);
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static DialectBackendTarget ParseMode(string mode)
    {
        return mode.Trim().ToLowerInvariant() switch
        {
            "compiler" => DialectBackendTarget.Cil,
            "interpreter" => DialectBackendTarget.Interpreter,
            _ => Thrower.InvalidOpEx<DialectBackendTarget>($"Unknown execution mode '{mode}'. Supported modes: 'compiler', 'interpreter'.")
        };
    }

    private static bool IsCoreForCompilationType(ICoreRunnable runnable, Type compilationType)
    {
        var type = runnable.GetType();
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
               type.GetGenericArguments()[0] == compilationType;
    }
}
