using DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Setup DI with auto-registration
var services = new ServiceCollection();
services.AddWistServices();

// Add optional modules
services.AddSingleton<IFrontendCoreModule>(new ExecutorDebugLoggerImpl());
services.AddSingleton<IFrontendCoreModule>(new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
services.AddSingleton<IFrontendCoreModule>(new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

var provider = services.BuildServiceProvider();
var core = provider.GetServices<ICoreRunnable>().First();


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