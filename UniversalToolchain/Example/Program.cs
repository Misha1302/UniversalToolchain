using System.Diagnostics;
using DependencyInjection;
using ObjectExtensions;

// Setup DI with auto-registration
var services = new ServiceCollection();

services.AddWistServices(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

// Add optional modules
services.AddSingleton<IFrontendCoreModule>(new ExecutorDebugLoggerImpl());
services.AddSingleton<IFrontendCoreModule>(new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
services.AddSingleton<IFrontendCoreModule>(new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

var provider = services.BuildServiceProvider();
var core = provider.GetServices<ICoreOptimizedRunnable>().First();


core.PrepareToRun(
    """
    3 + 4 * 5
    """
);

var sw = Stopwatch.StartNew();
var w = 0.0;
for (var i = 0; i < 10_000_000; i++)
{
    w += core.RunPrepared()!.Get<int>();
}
Console.WriteLine(w);
Console.WriteLine("Elapsed time: " + sw.Elapsed);