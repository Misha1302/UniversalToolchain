using BasicStdLib;
using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

[TestFixture]
public abstract class TestBase
{
    protected const int CoresCount = 2;
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Builds service provider with default configuration
    /// </summary>
    protected IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Auto-register all services
        services.AddWistServices();
        
        // Configure modules
        _serviceProvider = services.BuildServiceProvider();
        
        return _serviceProvider;
    }

    /// <summary>
    /// Executes code using the core runnable
    /// </summary>
    protected object ExecuteCode(string code)
    {
        if (_serviceProvider == null)
        {
            BuildServiceProvider();
        }

        var cores = _serviceProvider!.GetServices<ICoreRunnable>().ToList();
        var values = cores.Select(core => core.Run(code)).ToList();

        Thrower.AssertAlways(values.All(value => value?.Equals(values[0]) ?? value == values[0]));
        return values[0]!;
    }

    /// <summary>
    /// Creates a core instance of specific type
    /// </summary>
    protected T CreateCore<T>() where T : ICoreRunnable
    {
        if (_serviceProvider == null)
        {
            BuildServiceProvider();
        }

        return _serviceProvider!.GetServices<ICoreRunnable>()
            .OfType<T>()
            .FirstOrDefault()
            .NotNull($"Core of type {typeof(T).Name} not found");
    }
}