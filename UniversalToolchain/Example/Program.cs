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
    (System.Math.Abs(a - b) < 0.001) and
    (System.Math.Sin(a) > 0.5) and
    (System.Math.Cos(b) < 0.5) or
    (a * b > 10.0)
    """,
    new Dictionary<string, Type>
    {
        { "a", typeof(double) },
        { "b", typeof(double) }
    }
);

var fastCallable = new DynamicMethodInvoker<double, double, int>(executable);


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