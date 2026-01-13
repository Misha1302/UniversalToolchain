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


var executable = core.GetExecutable(
    """
    // Variables, native arithmetic, conditions
    let x = 42
    let y = 3.14f * 2.0f // Native float arithmetic
    
    if x > 10 and y < 10.0f (
        let result = (x + 5) * 2
        System.Console.WriteLine(result)
    )
    else (
        System.Console.WriteLine(-1)
    )
    
    // Loop via labels and goto
    @loop_start:
    if x > 0 (
        x = x - 1
        goto @loop_start
    )
    
    x // Implicit return
    """,
    new Dictionary<string, Type>
    {
        { "a", typeof(double) },
        { "b", typeof(double) }
    }
);

var fastCallable = new DynamicMethodInvoker<double, double, int>(executable);
Console.WriteLine(fastCallable.Invoke(5, 6));
return;

const int runsCount = 100_000_000;
Run(fastCallable,
    runsCount,
    out var oneOpNanoseconds,
    out var totalMilliseconds
);
Console.WriteLine($"{totalMilliseconds}ms;\t {oneOpNanoseconds}ns for 1 op");

return;

void Run(
    DynamicMethodInvoker<double, double, int> invoker,
    int count,
    out decimal oneOpNanoseconds,
    out decimal totalMilliseconds,
    bool warmup = true
)
{
    if (warmup)
        Run(invoker, Math.Max(10, count / 1000), out _, out _, false);

    var sw = Stopwatch.StartNew();
    for (var i = 0; i < count; i++)
        invoker.Invoke(1, 1.001);

    oneOpNanoseconds = (decimal)sw.ElapsedMilliseconds * 1_000_000 / count;
    totalMilliseconds = sw.ElapsedMilliseconds;
}