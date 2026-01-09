using System.Diagnostics;
using DependencyInjection;
using DynamicMethodCalling;

// Setup DI with auto-registration
var services = new ServiceCollection();

services.AddWistServices(options =>
    options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Universal
);

// Add optional modules
services.AddSingleton<IFrontendCoreModule>(new ExecutorDebugLoggerImpl());
services.AddSingleton<IFrontendCoreModule>(new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
services.AddSingleton<IFrontendCoreModule>(new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

var provider = services.BuildServiceProvider();
var core = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();
var dynamicMethod = core.GetExecutable(
    """
    (System.Math.Abs(a - b) < 0.001) and
    (System.Math.Sin(a) > 0.5) and
    (System.Math.Cos(b) < 0.5) or
    (a * b > 10.0)
    """,
    new Dictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } }
);
var fastCallable = new DynamicMethodInvoker<double, double, double>(dynamicMethod);

var sw = Stopwatch.StartNew();
for (var i = 0; i < 1_000_000; i++)
    fastCallable.Invoke(1.0, 1.001);
Console.WriteLine(sw.Elapsed);


public class NCalcContext
{
    public int Int1 { get; set; }
    public int Int2 { get; set; }
    public int Int3 { get; set; }
    public int Int4 { get; set; }
    public int Int5 { get; set; }
    public double Double1 { get; set; }
    public double Double2 { get; set; }
    public double Double3 { get; set; }
    public double Double4 { get; set; }
    public double Double5 { get; set; }
    public decimal Decimal1 { get; set; }
    public decimal Decimal2 { get; set; }
    public bool Bool1 { get; set; }
    public bool Bool2 { get; set; }
    public string String1 { get; set; }

    public int AddInts(int a, int b) => a + b;
    public double AddDoubles(double a, double b) => a + b;
    public double CalculateHypotenuse(double a, double b) => Math.Sqrt(a * a + b * b);
    public bool IsPositive(int x) => x > 0;
    public double CalculateTax(double amount, double rate) => amount * rate;
}