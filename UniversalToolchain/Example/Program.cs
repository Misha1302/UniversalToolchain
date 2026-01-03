using DependencyInjection;

// Setup DI with auto-registration
var services = new ServiceCollection();

services.AddWistServices(options =>
    options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Universal
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
        let counter = 0
        let total = 0

        @outer:
        if counter >= 3 goto @done
            let inner = 0
            
            @inner:
            if inner >= 3 goto @inner_done
                let x = counter * 10 + inner
                let y = Main.Pow(x, 2)
                
                if y > 100
                    total = total + Main.Sqrt(y)
                else
                    total = total + y
                
                inner = inner + 1
                goto @inner
            @inner_done:
            
            counter = counter + 1
            goto @outer
        @done:

        let result = Main.Round(total * 100) / 100
        result
        """
    )
);