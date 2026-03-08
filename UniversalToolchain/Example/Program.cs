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
    System.Console.WriteLine((if (a < 5 or b > 7) 5 else 3))
    """;


// // INTERPRETER
// var core = provider.GetService<ICoreRunnable>().NotNull();
// Console.WriteLine("Runned: " + core.Run(code, new Dictionary<string, object>
// {
//     ["a"] = 5,
//     ["b"] = 7
// }));



// COMPILER
var core = provider.GetService<IExecutableGiver<DynamicMethod>>().NotNull();
var method = core.GetExecutable(code, new());
var fastCallable = new DynamicMethodInvoker<int, int, int>(method);

Console.WriteLine("Result: " + fastCallable.Invoke(7, 15));
