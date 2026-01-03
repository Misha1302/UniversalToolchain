using DependencyInjection;

// Setup DI with auto-registration
var services = new ServiceCollection();

services.AddWistServices(options =>
    options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native
);

// Add optional modules
services.AddSingleton<IFrontendCoreModule>(new ExecutorDebugLoggerImpl());
services.AddSingleton<IFrontendCoreModule>(new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
services.AddSingleton<IFrontendCoreModule>(new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

var provider = services.BuildServiceProvider();
var core = provider.GetServices<ICoreRunnable>().First();

Console.WriteLine(
    core.Run(
        """
        let a = 5
        let b = 10
        let c = 15
        let result = 0
        
        if (a < b) and (b < c)
            result = 1
        else
            result = 0
        
        result
        """
    )
);