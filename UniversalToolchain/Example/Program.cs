using System.Diagnostics;
using DependencyInjection;
using DynamicMethodCalling;

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
var dynamicMethod = core.GetExecutable(
    """
    let c = a + b
    c
    """,
    new Dictionary<string, Type> { { "a", typeof(decimal) }, { "b", typeof(decimal) } }
);
var fastCallable = new DynamicMethodInvoker<decimal, decimal, decimal>(dynamicMethod);

var sw = Stopwatch.StartNew();
for (var i = 0; i < 100_000_000; i++)
    fastCallable.Invoke(1.0m, 1.001m);
Console.WriteLine(sw.Elapsed);