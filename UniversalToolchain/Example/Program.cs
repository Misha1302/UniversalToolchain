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


var complexMethod = core.GetExecutable(
    """
    (a > b) and (c > 10) or (a + b > c)
    """,
    new OrderedDictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) }, { "c", typeof(int) } }
);


var fastCallable = new DynamicMethodInvoker<int, int, int, bool>(complexMethod);
Console.WriteLine(fastCallable.Invoke(8, 15, 20));