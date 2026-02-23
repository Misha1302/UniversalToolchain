// Setup DI with auto-registration

using LoopsModule;

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
    fn add(a, b) (
        return a + b
    )
    
    add(2, 3)
    """,
    new OrderedDictionary<string, Type>());


var fastCallable = new DynamicMethodInvoker<int, int, int>(method);
Console.WriteLine("Result: " + fastCallable.Invoke(7, 15));