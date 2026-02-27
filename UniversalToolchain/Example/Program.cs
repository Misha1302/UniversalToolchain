var services = new ServiceCollection();

services.AddWistServices(options =>
    options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native
);

services.AddSingleton<IFrontendCoreModule>(new ExecutorDebugLoggerImpl());
services.AddSingleton<IFrontendCoreModule>(new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
services.AddSingleton<IFrontendCoreModule>(new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

var provider = services.BuildServiceProvider();

const string code =
    """
    i = 5
    while i < 25 ( i = i * 2 )
    i
    """;


// INTERPRETER
var core = provider.GetService<ICoreRunnable>().NotNull();
Console.WriteLine("Runned: " + core.Run(code));



/*
// COMPILER
var core = provider.GetService<IExecutableGiver<DynamicMethod>>().NotNull();
var method = core.GetExecutable(code, new());
var fastCallable = new DynamicMethodInvoker<int, int, int>(method);

Console.WriteLine("Result: " + fastCallable.Invoke(7, 15));
*/