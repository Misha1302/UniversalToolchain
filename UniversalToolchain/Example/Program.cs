using System.Diagnostics;
using DependencyInjection;
using DynamicMethodCalling;
using InternalPreprocessorLexemesModule;

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
var dynamicMethod = core.GetExecutable("2 * x", new Dictionary<string, Type> { { "x", typeof(int) } });
var fastCallable = new DynamicMethodInvoker<int, int>(dynamicMethod);

var w = 0.0;
var sw = Stopwatch.StartNew();
for (var i = 0; i < 1_000; i++)
{
    w += fastCallable.Invoke(5);
}
Console.WriteLine(w);
Console.WriteLine("Elapsed time: " + sw.Elapsed);