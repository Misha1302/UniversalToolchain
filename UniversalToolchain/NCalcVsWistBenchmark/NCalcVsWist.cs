using System.Reflection.Emit;
using BasicCore;
using BenchmarkDotNet.Attributes;
using DependencyInjection;
using DynamicMethodCalling;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NCalc;
using NCalc.LambdaCompilation;

namespace NCalcVsWistBenchmark;

[MemoryDiagnoser]
[RankColumn]
public class NCalcVsWist
{
    private readonly string _code = "3 + 4 * 5";

    private Func<int> _ncalcInvoker = null!;
    private DynamicMethodInvoker<int> _wistInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Инициализация DI для Wist выполняется один раз
        var services = new ServiceCollection().AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls
        );
        var provider = services.BuildServiceProvider();
        var method = provider
            .GetService<IExecutableGiver<DynamicMethod>>()
            .NotNull()
            .GetExecutable(_code);
        _wistInvoker = new DynamicMethodInvoker<int>(method);

        // Компиляция выражения NCalc также один раз
        var expression = new Expression(_code);
        _ncalcInvoker = expression.ToLambda<int>();

        // Проверка эквивалентности результатов
        var wistResult = _wistInvoker.Invoke();
        var ncalcResult = _ncalcInvoker.Invoke();
        Thrower.AssertAlways(wistResult == ncalcResult, "Результаты Wist и NCalc должны совпадать.");
    }

    [Benchmark(Baseline = true)]
    public int WistRun() => _wistInvoker.Invoke();

    [Benchmark]
    public int NCalcRun() => _ncalcInvoker.Invoke();
}

[MemoryDiagnoser]
[RankColumn]
public class SimpleAdditionBenchmark
{
    private Func<NCalcContext, int> _ncalcAddFunc = null!;
    private NCalcContext _ncalcContext = null!;
    private DynamicMethodInvoker<int, int, int> _wistAddInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Wist
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        var addMethod = core.GetExecutable("a + b",
            new Dictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) } });
        _wistAddInvoker = new DynamicMethodInvoker<int, int, int>(addMethod);

        // NCalc
        var addExpr = new Expression("[Int1] + [Int2]");
        _ncalcAddFunc = addExpr.ToLambda<NCalcContext, int>();

        _ncalcContext = new NCalcContext
        {
            Int1 = 5,
            Int2 = 3
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(_wistAddInvoker.Invoke(5, 3) == _ncalcAddFunc(_ncalcContext));
    }

    [Benchmark(Baseline = true)]
    public int Wist_SimpleAddition() => _wistAddInvoker.Invoke(5, 3);

    [Benchmark]
    public int NCalc_SimpleAddition() => _ncalcAddFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class ComplexArithmeticBenchmark
{
    private Func<NCalcContext, double> _ncalcComplexArithmeticFunc = null!;
    private NCalcContext _ncalcContext = null!;
    private DynamicMethodInvoker<double, double, double> _wistComplexArithmeticInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Wist
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        // Для комплексного выражения используем double, чтобы избежать целочисленного деления
        var complexMethod = core.GetExecutable("(a * 3.0 + b * 2.0) / (a - b + 1.0)",
            new Dictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } });
        _wistComplexArithmeticInvoker = new DynamicMethodInvoker<double, double, double>(complexMethod);

        // NCalc
        var complexExpr = new Expression("([Int1] * 3.0 + [Int2] * 2.0) / ([Int1] - [Int2] + 1.0)");
        _ncalcComplexArithmeticFunc = complexExpr.ToLambda<NCalcContext, double>();

        _ncalcContext = new NCalcContext
        {
            Int1 = 10,
            Int2 = 4
        };

        // Проверка эквивалентности
        var wistComplex = _wistComplexArithmeticInvoker.Invoke(10.0, 4.0);
        var ncalcComplex = _ncalcComplexArithmeticFunc(_ncalcContext);
        Thrower.AssertAlways(Math.Abs(wistComplex - ncalcComplex) < 1e-10);
    }

    [Benchmark(Baseline = true)]
    public double Wist_ComplexArithmetic() => _wistComplexArithmeticInvoker.Invoke(10.0, 4.0);

    [Benchmark]
    public double NCalc_ComplexArithmetic() => _ncalcComplexArithmeticFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class DoubleAdditionBenchmark
{
    private NCalcContext _ncalcContext = null!;
    private Func<NCalcContext, double> _ncalcDoubleAddFunc = null!;
    private DynamicMethodInvoker<double, double, double> _wistDoubleAddInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        // Double addition
        var doubleAdd = core.GetExecutable("a + b",
            new Dictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } });
        _wistDoubleAddInvoker = new DynamicMethodInvoker<double, double, double>(doubleAdd);

        // NCalc
        var doubleAddExpr = new Expression("[Double1] + [Double2]");
        _ncalcDoubleAddFunc = doubleAddExpr.ToLambda<NCalcContext, double>();

        _ncalcContext = new NCalcContext
        {
            Double1 = 3.14159,
            Double2 = 2.71828
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(Math.Abs(_wistDoubleAddInvoker.Invoke(3.14159, 2.71828) - _ncalcDoubleAddFunc(_ncalcContext)) < 1e-10);
    }

    [Benchmark(Baseline = true)]
    public double Wist_DoubleAddition() => _wistDoubleAddInvoker.Invoke(3.14159, 2.71828);

    [Benchmark]
    public double NCalc_DoubleAddition() => _ncalcDoubleAddFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class ComplexDoubleExpressionBenchmark
{
    private NCalcContext _ncalcContext = null!;
    private Func<NCalcContext, double> _ncalcDoubleComplexFunc = null!;
    private DynamicMethodInvoker<double, double, double, double> _wistDoubleComplexInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        // Complex double expression
        var doubleComplex = core.GetExecutable("(a * b) / (c + 1.0) + System.Math.Sin(a)",
            new Dictionary<string, Type>
            {
                { "a", typeof(double) },
                { "b", typeof(double) },
                { "c", typeof(double) }
            });
        _wistDoubleComplexInvoker = new DynamicMethodInvoker<double, double, double, double>(doubleComplex);

        // NCalc
        var doubleComplexExpr = new Expression("([Double1] * [Double2]) / ([Double3] + 1.0) + Sin([Double1])");
        _ncalcDoubleComplexFunc = doubleComplexExpr.ToLambda<NCalcContext, double>();

        _ncalcContext = new NCalcContext
        {
            Double1 = 2.0,
            Double2 = 3.0,
            Double3 = 4.0
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(Math.Abs(_wistDoubleComplexInvoker.Invoke(2.0, 3.0, 4.0) - _ncalcDoubleComplexFunc(_ncalcContext)) < 1e-10);
    }

    [Benchmark(Baseline = true)]
    public double Wist_ComplexDoubleExpression() => _wistDoubleComplexInvoker.Invoke(2.0, 3.0, 4.0);

    [Benchmark]
    public double NCalc_ComplexDoubleExpression() => _ncalcDoubleComplexFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class DecimalAdditionBenchmark
{
    private NCalcContext _ncalcContext = null!;
    private Func<NCalcContext, decimal> _ncalcDecimalAddFunc = null!;
    private DynamicMethodInvoker<decimal, decimal> _wistDecimalAddInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        var decimalAdd = core.GetExecutable(
            MakeCode("a"),
            new Dictionary<string, Type> { { "a", typeof(decimal) } }
        );

        _wistDecimalAddInvoker = new DynamicMethodInvoker<decimal, decimal>(decimalAdd);

        // NCalc
        var decimalAddExpr = new Expression(MakeCode("[Decimal1]"));
        _ncalcDecimalAddFunc = decimalAddExpr.ToLambda<NCalcContext, decimal>();

        _ncalcContext = new NCalcContext
        {
            Decimal1 = 123.456m
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(_wistDecimalAddInvoker.Invoke(123.456m) == _ncalcDecimalAddFunc(_ncalcContext));


        string MakeCode(string paramName)
        {
            const int additionsCountA = 3;
            const int additionsCountScopes1 = 3;
            const int additionsCountScopes2 = 3;
            const int additionsCountScopes3 = 4;
            const int additionsCountScopes4 = 4;

            return string.Join(" + ",
                Enumerable.Repeat(
                    "(" + string.Join(
                        " + ",
                        Enumerable.Repeat(
                            "(" + string.Join(
                                " + ",
                                Enumerable.Repeat(
                                    "(" + string.Join(
                                        " + ",
                                        Enumerable.Repeat(
                                            "(" + string.Join(" + ", Enumerable.Repeat(paramName, additionsCountA)) + ")",
                                            additionsCountScopes1
                                        )
                                    ) + ")",
                                    additionsCountScopes2
                                )
                            ) + ")",
                            additionsCountScopes3
                        )
                    ) + ")",
                    additionsCountScopes4
                )
            );
        }
    }

    [Benchmark(Baseline = true)]
    public decimal Wist_DecimalAddition() => _wistDecimalAddInvoker.Invoke(123.456m);

    [Benchmark]
    public decimal NCalc_DecimalAddition() => _ncalcDecimalAddFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class MathFunctionsBenchmark
{
    private NCalcContext _ncalcContext = null!;
    private Func<NCalcContext, double> _ncalcMathFunctionsFunc = null!;
    private DynamicMethodInvoker<double, double, double, double> _wistMathFunctionsInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        var mathFuncs = core.GetExecutable("System.Math.Pow(a, b) + System.Math.Sqrt(c)",
            new Dictionary<string, Type>
            {
                { "a", typeof(double) },
                { "b", typeof(double) },
                { "c", typeof(double) }
            });
        _wistMathFunctionsInvoker = new DynamicMethodInvoker<double, double, double, double>(mathFuncs);

        var mathExpr = new Expression("Pow([Double1], [Double2]) + Sqrt([Double3])");
        _ncalcMathFunctionsFunc = mathExpr.ToLambda<NCalcContext, double>();

        _ncalcContext = new NCalcContext
        {
            Double1 = 2.0,
            Double2 = 3.0,
            Double3 = 16.0
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(Math.Abs(_wistMathFunctionsInvoker.Invoke(2.0, 3.0, 16.0) - _ncalcMathFunctionsFunc(_ncalcContext)) < 1e-10);
    }

    [Benchmark(Baseline = true)]
    public double Wist_MathFunctions() => _wistMathFunctionsInvoker.Invoke(2.0, 3.0, 16.0);

    [Benchmark]
    public double NCalc_MathFunctions() => _ncalcMathFunctionsFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class SimpleConditionalBenchmark
{
    private Func<NCalcContext, int> _ncalcConditionalFunc = null!;
    private NCalcContext _ncalcContext = null!;
    private DynamicMethodInvoker<int, int, int> _wistConditionalInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        // Простое условие
        var conditionalCode = "if a > b a else b";
        var conditionalMethod = core.GetExecutable(conditionalCode,
            new Dictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) } });
        _wistConditionalInvoker = new DynamicMethodInvoker<int, int, int>(conditionalMethod);

        // NCalc
        var conditionalExpr = new Expression("if([Int1] > [Int2], [Int1], [Int2])");
        _ncalcConditionalFunc = conditionalExpr.ToLambda<NCalcContext, int>();

        _ncalcContext = new NCalcContext
        {
            Int1 = 8,
            Int2 = 12
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(_wistConditionalInvoker.Invoke(8, 12) == _ncalcConditionalFunc(_ncalcContext));
    }

    [Benchmark(Baseline = true)]
    public int Wist_SimpleConditional() => _wistConditionalInvoker.Invoke(8, 12);

    [Benchmark]
    public int NCalc_SimpleConditional() => _ncalcConditionalFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class ComplexConditionalBenchmark
{
    private Func<NCalcContext, double> _ncalcComplexConditionalFunc = null!;
    private NCalcContext _ncalcContext = null!;
    private DynamicMethodInvoker<double, double, double> _wistComplexConditionalInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        // Сложное условие
        var complexConditionalCode = """
                                     if a + b > 10.0 and a * b < 50.0
                                         System.Math.Sqrt(a + b)
                                     elif a + b > 5.0
                                         System.Math.Pow(a + b, 2.0)
                                     else
                                         a * b
                                     """;
        var complexMethod = core.GetExecutable(complexConditionalCode,
            new Dictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } });
        _wistComplexConditionalInvoker = new DynamicMethodInvoker<double, double, double>(complexMethod);

        // NCalc
        var complexExpr = new Expression(
            "if([Double1] + [Double2] > 10 and [Double1] * [Double2] < 50, " +
            "Sqrt([Double1] + [Double2]), " +
            "if([Double1] + [Double2] > 5, " +
            "Pow([Double1] + [Double2], 2), " +
            "[Double1] * [Double2]))");
        _ncalcComplexConditionalFunc = complexExpr.ToLambda<NCalcContext, double>();

        _ncalcContext = new NCalcContext
        {
            Double1 = 3.0,
            Double2 = 4.0
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(Math.Abs(_wistComplexConditionalInvoker.Invoke(3.0, 4.0) - _ncalcComplexConditionalFunc(_ncalcContext)) < 1e-10);
    }

    [Benchmark(Baseline = true)]
    public double Wist_ComplexConditional() => _wistComplexConditionalInvoker.Invoke(3.0, 4.0);

    [Benchmark]
    public double NCalc_ComplexConditional() => _ncalcComplexConditionalFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class MultipleIntegerVariablesBenchmark
{
    private NCalcContext _ncalcContext = null!;
    private Func<NCalcContext, int> _ncalcMultiVarFunc = null!;
    private DynamicMethodInvoker<int, int, int, int, int, int> _wistMultiVarInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        var multiVarMethod = core.GetExecutable("(a + b) * (c - d) / e",
            new Dictionary<string, Type>
            {
                { "a", typeof(int) },
                { "b", typeof(int) },
                { "c", typeof(int) },
                { "d", typeof(int) },
                { "e", typeof(int) }
            });
        _wistMultiVarInvoker = new DynamicMethodInvoker<int, int, int, int, int, int>(multiVarMethod);

        var multiVarExpr = new Expression("([Int1] + [Int2]) * ([Int3] - [Int4]) / [Int5]");
        _ncalcMultiVarFunc = multiVarExpr.ToLambda<NCalcContext, int>();

        _ncalcContext = new NCalcContext
        {
            Int1 = 10,
            Int2 = 20,
            Int3 = 30,
            Int4 = 5,
            Int5 = 2
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(_wistMultiVarInvoker.Invoke(10, 20, 30, 5, 2) == _ncalcMultiVarFunc(_ncalcContext));
    }

    [Benchmark(Baseline = true)]
    public int Wist_MultipleIntegerVariables() => _wistMultiVarInvoker.Invoke(10, 20, 30, 5, 2);

    [Benchmark]
    public int NCalc_MultipleIntegerVariables() => _ncalcMultiVarFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class BooleanLogicBenchmark
{
    private Func<NCalcContext, bool> _ncalcBooleanLogicFunc = null!;
    private NCalcContext _ncalcContext = null!;
    private DynamicMethodInvoker<int, int, int, bool> _wistBooleanLogicInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        var booleanLogicCode = "(a > b) and (c > 10) or (a + b > c)";
        var booleanMethod = core.GetExecutable(booleanLogicCode,
            new Dictionary<string, Type>
            {
                { "a", typeof(int) },
                { "b", typeof(int) },
                { "c", typeof(int) }
            });
        _wistBooleanLogicInvoker = new DynamicMethodInvoker<int, int, int, bool>(booleanMethod);

        var booleanExpr = new Expression("([Int1] > [Int2]) and ([Int3] > 10) or ([Int1] + [Int2] > [Int3])");
        _ncalcBooleanLogicFunc = booleanExpr.ToLambda<NCalcContext, bool>();

        _ncalcContext = new NCalcContext
        {
            Int1 = 15,
            Int2 = 8,
            Int3 = 20
        };

        // Проверка эквивалентности
        // ReSharper disable once ArrangeRedundantParentheses
        Thrower.AssertAlways(_wistBooleanLogicInvoker.Invoke(15, 8, 20) == _ncalcBooleanLogicFunc(_ncalcContext));
    }

    [Benchmark(Baseline = true)]
    public bool Wist_BooleanLogic() => _wistBooleanLogicInvoker.Invoke(15, 8, 20);

    [Benchmark]
    public bool NCalc_BooleanLogic() => _ncalcBooleanLogicFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class ComplexBooleanBenchmark
{
    private Func<NCalcContext, bool> _ncalcComplexBooleanFunc = null!;
    private NCalcContext _ncalcContext = null!;
    private DynamicMethodInvoker<double, double, bool> _wistComplexBooleanInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        var complexBooleanCode = """
                                 (System.Math.Abs(a - b) < 0.001) and
                                 (System.Math.Sin(a) > 0.5) and
                                 (System.Math.Cos(b) < 0.5) or
                                 (a * b > 10.0)
                                 """;
        var complexMethod = core.GetExecutable(complexBooleanCode,
            new Dictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } });
        _wistComplexBooleanInvoker = new DynamicMethodInvoker<double, double, bool>(complexMethod);

        var complexExpr = new Expression(
            "(Abs([Double1] - [Double2]) < 0.001) and " +
            "(Sin([Double1]) > 0.5) and " +
            "(Cos([Double2]) < 0.5) or " +
            "([Double1] * [Double2] > 10.0)");
        _ncalcComplexBooleanFunc = complexExpr.ToLambda<NCalcContext, bool>();

        _ncalcContext = new NCalcContext
        {
            Double1 = 1.0,
            Double2 = 1.001
        };

        // Проверка эквивалентности
        // ReSharper disable once ArrangeRedundantParentheses
        Thrower.AssertAlways(_wistComplexBooleanInvoker.Invoke(1.0, 1.001) == _ncalcComplexBooleanFunc(_ncalcContext));
    }

    [Benchmark(Baseline = true)]
    public bool Wist_ComplexBoolean() => _wistComplexBooleanInvoker.Invoke(1.0, 1.001);

    [Benchmark]
    public bool NCalc_ComplexBoolean() => _ncalcComplexBooleanFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class TaxCalculationBenchmark
{
    private NCalcContext _ncalcContext = null!;
    private Func<NCalcContext, double> _ncalcTaxCalculationFunc = null!;
    private DynamicMethodInvoker<double, double, double> _wistTaxCalculationInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        var taxCode = """
                      let tax = amount * rate
                      let total = amount + tax
                      if amount > 1000.0
                          total * 0.95
                      else
                          total
                      """;
        var taxMethod = core.GetExecutable(taxCode,
            new Dictionary<string, Type> { { "amount", typeof(double) }, { "rate", typeof(double) } });
        _wistTaxCalculationInvoker = new DynamicMethodInvoker<double, double, double>(taxMethod);

        var taxExpr = new Expression(
            "if([Double1] > 1000.0, " +
            "([Double1] + [Double1] * [Double2]) * 0.95, " +
            "[Double1] + [Double1] * [Double2])");
        _ncalcTaxCalculationFunc = taxExpr.ToLambda<NCalcContext, double>();

        _ncalcContext = new NCalcContext
        {
            Double1 = 1500.0,
            Double2 = 0.2
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(Math.Abs(_wistTaxCalculationInvoker.Invoke(1500.0, 0.2) - _ncalcTaxCalculationFunc(_ncalcContext)) < 1e-10);
    }

    [Benchmark(Baseline = true)]
    public double Wist_TaxCalculation() => _wistTaxCalculationInvoker.Invoke(1500.0, 0.2);

    [Benchmark]
    public double NCalc_TaxCalculation() => _ncalcTaxCalculationFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class CompoundInterestBenchmark
{
    private NCalcContext _ncalcContext = null!;
    private Func<NCalcContext, double> _ncalcFinancialFunc = null!;
    private DynamicMethodInvoker<double, double, double, double, double> _wistFinancialInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        var financialCode = """
                            let result = principal * System.Math.Pow(1.0 + rate / periods, periods * years)
                            System.Math.Round(result, 2)
                            """;
        var financialMethod = core.GetExecutable(financialCode,
            new Dictionary<string, Type>
            {
                { "principal", typeof(double) },
                { "rate", typeof(double) },
                { "periods", typeof(double) },
                { "years", typeof(double) }
            });
        _wistFinancialInvoker = new DynamicMethodInvoker<double, double, double, double, double>(financialMethod);

        var financialExpr = new Expression(
            "Round([Double1] * Pow(1.0 + [Double2] / [Double3], [Double3] * [Double4]), 2)");
        _ncalcFinancialFunc = financialExpr.ToLambda<NCalcContext, double>();

        _ncalcContext = new NCalcContext
        {
            Double1 = 1000.0,
            Double2 = 0.05,
            Double3 = 12.0,
            Double4 = 10.0
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(Math.Abs(_wistFinancialInvoker.Invoke(1000.0, 0.05, 12.0, 10.0) - _ncalcFinancialFunc(_ncalcContext)) < 1e-10);
    }

    [Benchmark(Baseline = true)]
    public double Wist_CompoundInterest() => _wistFinancialInvoker.Invoke(1000.0, 0.05, 12.0, 10.0);

    [Benchmark]
    public double NCalc_CompoundInterest() => _ncalcFinancialFunc(_ncalcContext);
}

[MemoryDiagnoser]
[RankColumn]
public class LargeExpressionBenchmark
{
    private LargeExpressionContext _context = null!;
    private Func<LargeExpressionContext, double> _ncalcLargeExpressionFunc = null!;
    private DynamicMethodInvoker<double, double, double, double, double> _wistLargeExpressionInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddWistServices(
            options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native,
            GlobalPath.PathToDlls);
        var provider = services.BuildServiceProvider();
        var core = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        const string largeExpression = """
                                       let x = a * System.Math.Sin(b) + System.Math.Cos(c) * System.Math.Tan(d)
                                       let y = System.Math.Pow(a, 2.0) + System.Math.Pow(b, 3.0) - System.Math.Pow(c, 0.5)
                                       let z = System.Math.Exp(-0.5 * (System.Math.Pow(a - 1.0, 2.0) + System.Math.Pow(b - 2.0, 2.0) + System.Math.Pow(c - 3.0, 2.0)) / (d + 1.0))
                                       (x * y * z + System.Math.Log(System.Math.Abs(a) + 1.0) + System.Math.Log10(System.Math.Abs(b) + 1.0) + System.Math.Log(System.Math.Abs(c) + 1.0)) /
                                       (System.Math.Sqrt(x * x + y * y + z * z) + 1e-10)
                                       """;
        var largeMethod = core.GetExecutable(largeExpression,
            new Dictionary<string, Type>
            {
                { "a", typeof(double) },
                { "b", typeof(double) },
                { "c", typeof(double) },
                { "d", typeof(double) }
            });
        _wistLargeExpressionInvoker = new DynamicMethodInvoker<double, double, double, double, double>(largeMethod);

        const string ncalcExpression =
            @"(([a] * Sin([b]) + Cos([c]) * Tan([d])) *
           (Pow([a], 2) + Pow([b], 3) - Pow([c], 0.5)) *
           Exp(-0.5 * (Pow([a] - 1, 2) + Pow([b] - 2, 2) + Pow([c] - 3, 2)) / ([d] + 1)) +
           Log(Abs([a]) + 1, 2.718281828459045) + Log10(Abs([b]) + 1) + Log(Abs([c]) + 1, 2.718281828459045)) /
          (Sqrt(Pow([a] * Sin([b]) + Cos([c]) * Tan([d]), 2) +
                Pow(Pow([a], 2) + Pow([b], 3) - Pow([c], 0.5), 2) +
                Pow(Exp(-0.5 * (Pow([a] - 1, 2) + Pow([b] - 2, 2) + Pow([c] - 3, 2)) / ([d] + 1)), 2)) + 1e-10)";
        var ncalcExpr = new Expression(ncalcExpression);
        _ncalcLargeExpressionFunc = ncalcExpr.ToLambda<LargeExpressionContext, double>();

        _context = new LargeExpressionContext
        {
            a = 1.5,
            b = 2.5,
            c = 3.5,
            d = 4.5
        };

        // Проверка эквивалентности
        Thrower.AssertAlways(Math.Abs(_wistLargeExpressionInvoker.Invoke(1.5, 2.5, 3.5, 4.5) - _ncalcLargeExpressionFunc(_context)) < 1e-10);
    }

    [Benchmark(Baseline = true)]
    public double Wist_LargeExpression() => _wistLargeExpressionInvoker.Invoke(1.5, 2.5, 3.5, 4.5);

    [Benchmark]
    public double NCalc_LargeExpression() => _ncalcLargeExpressionFunc(_context);
}

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
public class LargeExpressionContext
{
    public double a { get; set; }
    public double b { get; set; }
    public double c { get; set; }
    public double d { get; set; }
}

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
    public decimal Decimal1 { get; set; }
}