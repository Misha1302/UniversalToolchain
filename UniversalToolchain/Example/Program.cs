// Setup DI with auto-registration

using BasicCore.Contracts;
using DynamicMethodCalling.Core;
using ParserConfigurationModule.Core;
using ParserConfigurationModule.Module;

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


var method = core.GetExecutable(
    """
    i = -5
    while i < 0 (i = i + 1)
    i
    """,
    new OrderedDictionary<string, Type> { ["a"] = typeof(int), ["b"] = typeof(int) });


var fastCallable = new DynamicMethodInvoker<int, int, int>(method);
Console.WriteLine("Result: " + fastCallable.Invoke(7, 15));