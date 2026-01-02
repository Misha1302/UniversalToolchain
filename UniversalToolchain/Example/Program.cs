using DependencyInjection;

// Setup DI with auto-registration
var services = new ServiceCollection();

services.AddWistServices(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

// Add optional modules
services.AddSingleton<IFrontendCoreModule>(new ExecutorDebugLoggerImpl());
services.AddSingleton<IFrontendCoreModule>(new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
services.AddSingleton<IFrontendCoreModule>(new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

var provider = services.BuildServiceProvider();
var core = provider.GetServices<ICoreRunnable>().First();

Console.WriteLine(
    core.Run(
        """
        let iterations = 1000
        let pi = 3.141592653589793
        let e = 2.718281828459045
        let sum = 0.0
        let i = 0
        @loop:
            if i >= iterations goto @end
            let angle = Main.ToDouble(i) * pi / Main.ToDouble(iterations)
            sum = sum + DoubleMath.Sin(angle) * DoubleMath.Cos(angle) * DoubleMath.Exp((0.0 - angle) / e)
            i = i + 1
            goto @loop
        @end:
        sum
        """
    )
);