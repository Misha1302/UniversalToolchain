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
        {
            BuildServiceProvider();
        }

        var cores = _serviceProvider!.GetServices<ICoreRunnable>().ToList();
        var values = cores.Select(core => core.Run(code)).ToList();

        foreach (var value in values)
        {
            Assert.That(value, Is.EqualTo(values[0]));
        }

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
        {
            BuildServiceProvider();
        }

        return _serviceProvider!.GetServices<ICoreRunnable>()
            .OfType<T>()
            .FirstOrDefault()
            .NotNull($"Core of type {typeof(T).Name} not found");
    }

    /// <summary>
    ///     Вспомогательный метод для сравнения чисел с учетом типа
    /// </summary>
    static protected void AssertNumbersEqual(object expected, object actual, double tolerance = 1e-9)
    {
        if (expected is double expectedDouble && actual is double actualDouble)
        {
            Assert.That(actualDouble, Is.EqualTo(expectedDouble).Within(tolerance));
        }
        else if (expected is float expectedFloat && actual is float actualFloat)
        {
            Assert.That(actualFloat, Is.EqualTo(expectedFloat).Within((float)tolerance));
        }
        else if (expected is decimal expectedDecimal && actual is decimal actualDecimal)
        {
            // Для decimal используем более высокую точность
            var decimalTolerance = (decimal)tolerance;
            Assert.That(actualDecimal, Is.EqualTo(expectedDecimal).Within(decimalTolerance));
        }
        else if (expected is int expectedInt && actual is int actualInt)
        {
            Assert.That(actualInt, Is.EqualTo(expectedInt));
        }
        else if (expected is long expectedLong && actual is long actualLong)
        {
            Assert.That(actualLong, Is.EqualTo(expectedLong));
        }
        else
        {
            // Для остальных типов используем стандартное сравнение
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}