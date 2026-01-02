using System.Diagnostics;
using DependencyInjection;
using NativeMathModule;
using ObjectExtensions;

// Setup DI with auto-registration
var services = new ServiceCollection();

services.AddWistServices();

// Add optional modules
services.AddSingleton<IFrontendCoreModule>(new ExecutorDebugLoggerImpl());
services.AddSingleton<IFrontendCoreModule>(new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
services.AddSingleton<IFrontendCoreModule>(new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

var provider = services.BuildServiceProvider();
var core = provider.GetServices<ICoreOptimizedRunnable>().First();


core.PrepareToRun(
    """
    let i = 0
    let sum = 0
    @start:
    if i > 100 goto @end
        sum = sum + i
        i = i + 1
        goto @start
    @end:
    sum
    """
);

var sw = Stopwatch.StartNew();
var w = 0.0;
for (int i = 0; i < 1_000_000_000; i++)
{
    w += core.RunPrepared()!.Get<RealNumberImpl>().GetValue();
}
Console.WriteLine(w);
Console.WriteLine("Elapsed time: " + sw.Elapsed);