using DependencyInjection;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

[TestFixture]
public abstract class TestBase
{
    protected const int CoresCount = 2;
    private IServiceProvider? _serviceProvider;
    private WistOptions.ArithmeticModeEnum _arithmeticMode = WistOptions.ArithmeticModeEnum.Universal;

    /// <summary>
    ///     Устанавливает режим арифметики для тестов
    /// </summary>
    protected void SetArithmeticMode(WistOptions.ArithmeticModeEnum mode)
    {
        _arithmeticMode = mode;
        _serviceProvider = null; // Сброс провайдера при изменении режима
    }

    /// <summary>
    ///     Создает сервис-провайдер с указанной конфигурацией арифметики
    /// </summary>
    virtual protected IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddWistServices(options => options.ArithmeticMode = _arithmeticMode);
        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }

    /// <summary>
    ///     Выполняет код и возвращает результат как динамический тип
    /// </summary>
    internal object ExecuteCode(string code)
    {
        if (_serviceProvider == null)
            BuildServiceProvider();

        var cores = _serviceProvider!.GetServices<ICoreRunnable>().ToList();
        var values = cores.Select(core => core.Run(code)).ToList();

        if (values.Any(x => x == null))
        {
            Assert.That(values.All(x => x == null));
            return null!;
        }

        var typedValues = values
            .Select(value => value!.GetType())
            .Select(type =>
            {
                try
                {
                    return values.Select(x => CastType(x!, type)!).ToList();
                }
                catch
                {
                    return null;
                }
            })
            .First(x => x != null)!;

        foreach (var value in typedValues)
            Assert.That(value, Is.EqualTo(typedValues[0]));

        return typedValues[0]!;
    }

    /// <summary>
    ///     Выполняет код и возвращает результат как указанный тип
    /// </summary>
    internal T ExecuteCode<T>(string code)
    {
        var result = ExecuteCode(code);
        return (T)CastType(result, typeof(T))!;
    }

    private static object? CastType(object value, Type t)
    {
        if (value.GetType() == t)
            return value;

        if (value is int i && t == typeof(bool))
            return i == 1;

        return Thrower.InvalidCast<object?>($"Cannot convert test result from type {value.GetType()} to {t}.");
    }

    /// <summary>
    ///     Создает ядро определенного типа
    /// </summary>
    protected T CreateCore<T>() where T : ICoreRunnable
    {
        if (_serviceProvider == null)
            BuildServiceProvider();

        return _serviceProvider!.GetServices<ICoreRunnable>()
            .OfType<T>()
            .FirstOrDefault()
            .NotNull($"Core of type {typeof(T).Name} not found");
    }
}