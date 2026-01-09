using DependencyInjection;
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

        foreach (var value in values)
            Assert.That(value, Is.EqualTo(values[0]));

        return values[0]!;
    }

    /// <summary>
    ///     Выполняет код и возвращает результат как указанный тип
    /// </summary>
    internal T ExecuteCode<T>(string code)
    {
        var result = ExecuteCode(code);

        if (result is T typedResult)
            return typedResult;

        // Пытаемся преобразовать
        try
        {
            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (InvalidCastException)
        {
            throw new InvalidCastException($"Cannot convert result of type {result.GetType()} to {typeof(T)}");
        }
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