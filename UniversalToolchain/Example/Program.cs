using System.Diagnostics;
using DependencyInjection;
using DynamicMethodCalling;
using ObjectExtensions;

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
var core = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();
var dynamicMethod = core.GetExecutable("3 + 4 * 5");
var fastCallable = new DynamicMethodInvoker<int>(dynamicMethod);
    
var w = 0.0;
Stopwatch sw = Stopwatch.StartNew();
for (int i = 0; i < 1_000_000_000; i++)
{
    w += fastCallable.Invoke();
}
Console.WriteLine(w);
Console.WriteLine("Elapsed time: " + sw.Elapsed);