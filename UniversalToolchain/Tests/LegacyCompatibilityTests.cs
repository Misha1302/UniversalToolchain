using Microsoft.Extensions.DependencyInjection;

namespace Tests;

[TestFixture]
public class LegacyCompatibilityTests : TestBase
{
    [Test]
    public void ExecuteCode_WithDIEnabled_WorksCorrectly()
    {
        // DI включен по умолчанию
        var code = "2 + 3 * 4";

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(14).Within(1e-9));
    }

    [Test]
    public void ExecuteCode_WithDIDisabled_UsesLegacyMode()
    {
        // Отключаем DI
        EnableDependencyInjection(false);

        var code = "2 + 3 * 4";

        var result = ExecuteCode(code);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(14).Within(1e-9));

        // Включаем DI обратно для других тестов
        EnableDependencyInjection(true);
    }

    [Test]
    public void ExecuteCode_WithMiddleEndModules_LogsWarning()
    {
        var code = "2 + 2";
        var middleEndModules = new Dictionary<Type, object>
        {
            { typeof(DynamicMethod), new List<IMiddleEndCoreModule<DynamicMethod>>() }
        };

        // Должен вывести предупреждение в Debug, но работать
        var result = ExecuteCode(code, middleEndModules);

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(4).Within(1e-9));
    }

    [Test]
    public void ConfigureTestServices_Override_AddsCustomServices()
    {
        // Создаем тестовый класс с переопределением ConfigureTestServices
        var testInstance = new TestClassWithCustomServices();

        var result = testInstance.ExecuteCode("5");

        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(5).Within(1e-9));
    }

    private class TestClassWithCustomServices : TestBase
    {
        override protected void ConfigureTestServices(IServiceCollection services)
        {
            // Добавляем кастомный сервис
            services.AddSingleton<ICustomService, CustomServiceImpl>();
        }

        [Test]
        public void CustomServiceTest()
        {
            var provider = BuildTestServiceProvider();
            var service = provider.GetService<ICustomService>();

            Assert.That(service, Is.Not.Null);
            Assert.That(service, Is.InstanceOf<CustomServiceImpl>());
        }
    }

    private interface ICustomService;

    private class CustomServiceImpl : ICustomService;
}