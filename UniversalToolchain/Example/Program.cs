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
    if a > b a else b
    """,
    new OrderedDictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) } }
);


var fastCallable = new DynamicMethodInvoker<int, int, int>(complexMethod);
Console.WriteLine(fastCallable.Invoke(8, 15));