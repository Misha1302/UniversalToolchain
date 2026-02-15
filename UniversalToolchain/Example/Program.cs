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


var method = core.GetExecutable(
    """
    let i = a, sum = 0
    @start:
        if i > b goto @end
        System.Console.WriteLine(i)
        sum = sum + i; i = i + 1 
        goto @start
    @end:
    sum
    """,
    new OrderedDictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) } }
);


var fastCallable = new DynamicMethodInvoker<int, int, int>(method);
Console.WriteLine("Result: " + fastCallable.Invoke(7, 15));