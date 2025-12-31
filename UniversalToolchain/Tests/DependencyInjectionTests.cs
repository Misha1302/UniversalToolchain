using BasicCore.TranslatorWrapper;
using DependencyInjection;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

[TestFixture]
public class DependencyInjectionTests : TestBase
{
    [Test]
    public void ExecuteCode_WithDI_ReturnsSameResultAsOriginal()
    {
        // Arrange
        var code = "2 + 3 * 4";

        // Act - using DI
        var diResult = ExecuteCodeWithDI(code);

        // Act - using original method
        var originalResult = ExecuteCode(code);

        // Assert
        var diNumberResult = (RealNumberImpl)diResult;
        var originalNumberResult = (RealNumberImpl)originalResult;

        Assert.That(diNumberResult.GetValue(),
            Is.EqualTo(originalNumberResult.GetValue()).Within(1e-9));
    }

    [Test]
    public void BuildTestServiceProvider_RegistersAllRequiredServices()
    {
        // Act
        var provider = BuildTestServiceProvider();

        // Assert
        Assert.That(provider.GetService<Func<ILexer>>(), Is.Not.Null);
        Assert.That(provider.GetService<Func<IParser>>(), Is.Not.Null);
        Assert.That(provider.GetService<Func<IAstToBytecodeTranslator>>(), Is.Not.Null);
        Assert.That(provider.GetService<Func<IAbstractMethodsTranslator>>(), Is.Not.Null);

        var modules = provider.GetServices<IFrontendCoreModule>().ToList();
        Assert.That(modules, Has.Count.GreaterThan(10)); // Should have all standard modules
    }

    [Test]
    public void CreateCoreWithDI_DynamicMethod_ReturnsValidCore()
    {
        // Arrange
        BuildTestServiceProvider();

        // Act
        var core = CreateCoreWithDI<DynamicMethod>();

        // Assert
        Assert.That(core, Is.Not.Null);
        Assert.That(core, Is.InstanceOf<BasicCoreImpl<DynamicMethod>>());
    }

    [Test]
    public void CreateCoreWithDI_AbstractIR_ReturnsValidCore()
    {
        // Arrange
        BuildTestServiceProvider();

        // Act
        var core = CreateCoreWithDI<IAbstractIR>();

        // Assert
        Assert.That(core, Is.Not.Null);
        Assert.That(core, Is.InstanceOf<BasicCoreImpl<IAbstractIR>>());
    }

    [Test]
    public void TestServiceProvider_BuildMinimalProvider_WorksCorrectly()
    {
        // Act
        var provider = TestServiceProvider.BuildMinimalTestProvider();

        // Assert
        Assert.That(provider.GetService<Func<ILexer>>(), Is.Not.Null);
        Assert.That(provider.GetService<Func<IParser>>(), Is.Not.Null);
    }

    [Test]
    public void TestServiceProvider_BuildProviderWithCustomModules_WorksCorrectly()
    {
        // Arrange
        var customModule = new CustomTestModule();

        // Act
        var provider = TestServiceProvider.BuildProviderWithModules(customModule);
        var modules = provider.GetServices<IFrontendCoreModule>().ToList();

        // Assert
        Assert.That(modules, Has.Count.EqualTo(1));
        Assert.That(modules[0], Is.InstanceOf<CustomTestModule>());
    }

    [Test]
    public void ConfigureTestServices_Override_AddsCustomServices()
    {
        // Arrange
        var testClass = new TestClassWithCustomServices();

        // Act
        var provider = testClass.BuildTestServiceProvider();
        var customService = provider.GetService<ICustomService>();

        // Assert
        Assert.That(customService, Is.Not.Null);
        Assert.That(customService, Is.InstanceOf<CustomServiceImpl>());
    }

    [Test]
    public void DependencyInjection_EnablesMocking()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistTestServices();

        // Mock lexer that always returns specific tokens
        services.AddTransient<Func<ILexer>>(_ => () => new MockLexer());
        services.AddCoreRunnables();

        var provider = services.BuildServiceProvider();

        // Act
        var core = provider.GetServices<ICoreRunnable>().First();
        var result = core.Run("test");

        // Assert - MockLexer returns number 42
        var numberResult = (RealNumberImpl?)result;
        Assert.That(numberResult?.GetValue(), Is.EqualTo(42).Within(1e-9));
    }

    // Mock implementations for testing
    private class MockLexer : ILexer
    {
        public LexerConfiguration Configuration { get; } = new([]);

        public List<LexemeValue> Lexemize(string code)
        {
            // Return a single number token
            var pattern = new LexemePattern(@"\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Number"));
            return [new LexemeValue("42", pattern, 0, code)];
        }
    }

    private class CustomTestModule : IFrontendCoreModule
    {
        public void InitLexer(ILexer lexer)
        {
        }

        public void InitParser(IParser parser)
        {
        }

        public void InitAstTranslator(IAstToBytecodeTranslator translator)
        {
        }
    }

    private class TestClassWithCustomServices : TestBase
    {
        override protected void ConfigureTestServices(IServiceCollection services)
        {
            services.AddSingleton<ICustomService, CustomServiceImpl>();
        }
    }

    private interface ICustomService;

    private class CustomServiceImpl : ICustomService;
}