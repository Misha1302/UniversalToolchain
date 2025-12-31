Main.LoadStdLibToThisAssembly();

// Создаем DI контейнер
var serviceCollection = new ServiceCollection();

// Регистрируем все сервисы и модули
ConfigureServices(serviceCollection);

var serviceProvider = serviceCollection.BuildServiceProvider();

// Получаем ядро через DI
var core = serviceProvider.GetRequiredService<ICoreRunnable>();

var result = core.Run(
    """
    let sum = 0
    let i = 1
    @loop:
    if i > 1000000 goto @end
        sum = sum + i
        i = i + 1
        goto @loop
    @end:
    sum
    """
);

Console.WriteLine(result);
return;


void ConfigureServices(IServiceCollection services)
{
    // Основные фабрики
    services.AddSingleton<Func<ILexer>>(_ => () => new BasicLexerImpl());
    services.AddSingleton<Func<IParser>>(_ => () => new BasicParserImpl());
    services.AddSingleton<Func<IAstToBytecodeTranslator>>(_ => () => new BasicAstToBytecodeTranslatorImpl());
    services.AddSingleton<Func<IAbstractMethodsTranslator>>(_ => () => new BytecodeToAbstractIrConverterImpl());
    services.AddSingleton<Func<IExecutor<DynamicMethod>>>(_ => () => new DynamicMethodExecutor());

    // Компиляторы
    services.AddSingleton<AbstractMethodsCompilerImpl>();

    // Все стандартные фронтенд-модули
    services.AddSingleton<IFrontendCoreModule, IdentifierModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, ScopesModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, NumbersModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, WhitespaceModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, SemicolonAsNewLineModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, ArithmeticModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, CSharpInteropModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, LabelsModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, VariablesModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, EqualityModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, ConditionsModuleImpl>();
    services.AddSingleton<IFrontendCoreModule, ComparisonOperations>();
    services.AddSingleton<IFrontendCoreModule, BooleanOperations>();

    // Дополнительные модули из примера
    services.AddSingleton<IFrontendCoreModule, LocalVariablesOptimizer>();
    services.AddSingleton<IFrontendCoreModule, ExecutorDebugLoggerImpl>();
    services.AddSingleton<IFrontendCoreModule>(_ =>
        new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
    services.AddSingleton<IFrontendCoreModule>(_ =>
        new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

    // Регистрируем BasicCoreImpl<DynamicMethod> как основной ICoreRunnable
    services.AddSingleton<ICoreRunnable>(provider =>
    {
        var modules = provider.GetServices<IFrontendCoreModule>().ToList();

        return new BasicCoreImpl<DynamicMethod>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => provider.GetRequiredService<AbstractMethodsCompilerImpl>(),
            provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>(),
            modules,
            [] // Middle-end modules
        );
    });
}