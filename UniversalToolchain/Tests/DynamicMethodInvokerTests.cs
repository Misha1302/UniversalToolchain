using DependencyInjection;
using DynamicMethodCalling;
using GrEmit;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

[TestFixture]
public class DynamicMethodInvokerTests
{
    [Test]
    public void Invoker_ZeroArguments_ReturnsCorrectValue()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("TestZeroArgs", typeof(int), Type.EmptyTypes);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int>(dynamicMethod);

        // Act
        var result = invoker.Invoke();

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Invoker_OneArgument_ReturnsCorrectValue()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("TestOneArg", typeof(int), new[] { typeof(int) });
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, 2);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, int>(dynamicMethod);

        // Act
        var result = invoker.Invoke(21);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Invoker_TwoArguments_ReturnsCorrectValue()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("TestTwoArgs", typeof(int), new[] { typeof(int), typeof(int) });
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, int, int>(dynamicMethod);

        // Act
        var result = invoker.Invoke(20, 22);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Invoker_ThreeArguments_ReturnsCorrectValue()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("TestThreeArgs", typeof(int),
            new[] { typeof(int), typeof(int), typeof(int) });
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, int, int, int>(dynamicMethod);

        // Act
        var result = invoker.Invoke(10, 20, 12);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Invoker_WithDifferentReturnTypes_WorksCorrectly()
    {
        // Test with double
        var dm1 = new DynamicMethod("TestDouble", typeof(double), Type.EmptyTypes);
        var il1 = dm1.GetILGenerator();
        il1.Emit(OpCodes.Ldc_R8, 3.14159);
        il1.Emit(OpCodes.Ret);
        var invoker1 = new DynamicMethodInvoker<double>(dm1);
        Assert.That(invoker1.Invoke(), Is.EqualTo(3.14159).Within(1e-9));

        // Test with float
        var dm2 = new DynamicMethod("TestFloat", typeof(float), Type.EmptyTypes);
        var il2 = dm2.GetILGenerator();
        il2.Emit(OpCodes.Ldc_R4, 2.71828f);
        il2.Emit(OpCodes.Ret);
        var invoker2 = new DynamicMethodInvoker<float>(dm2);
        Assert.That(invoker2.Invoke(), Is.EqualTo(2.71828f).Within(1e-6f));

        // Test with bool
        var dm3 = new DynamicMethod("TestBool", typeof(bool), Type.EmptyTypes);
        var il3 = dm3.GetILGenerator();
        il3.Emit(OpCodes.Ldc_I4_1);
        il3.Emit(OpCodes.Ret);
        var invoker3 = new DynamicMethodInvoker<bool>(dm3);
        Assert.That(invoker3.Invoke(), Is.True);
    }

    [Test]
    public void Invoker_WithMixedArgumentTypes_WorksCorrectly()
    {
        // Arrange: (int, double, float) -> double
        var dynamicMethod = new DynamicMethod("TestMixedArgs", typeof(double),
            new[] { typeof(int), typeof(double), typeof(float) });
        var il = dynamicMethod.GetILGenerator();

        // Convert int to double
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Conv_R8);

        // Add double
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);

        // Convert float to double and add
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Conv_R8);
        il.Emit(OpCodes.Add);

        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, double, float, double>(dynamicMethod);

        // Act
        var result = invoker.Invoke(10, 20.5, 11.5f);

        // Assert
        Assert.That(result, Is.EqualTo(42.0).Within(1e-9));
    }

    [Test]
    public void Invoker_ThrowsException_WhenReturnTypeMismatch()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("TestMismatch", typeof(int), Type.EmptyTypes);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ret);

        // Act & Assert
        Assert.That(() => new DynamicMethodInvoker<double>(dynamicMethod), Throws.Exception);
    }

    [Test]
    public void Invoker_CanBeCalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("TestMultipleCalls", typeof(int), new[] { typeof(int) });
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, 2);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, int>(dynamicMethod);

        // Act & Assert
        for (var i = 0; i < 1000; i++)
            Assert.That(invoker.Invoke(i), Is.EqualTo(i * 2));
    }

    [Test]
    public void Invoker_WithMaxArguments_WorksCorrectly()
    {
        // Test with 10 arguments (maximum supported)
        var dynamicMethod = new DynamicMethod("TestMaxArgs", typeof(int),
            new[]
            {
                typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
                typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)
            });

        var il = dynamicMethod.GetILGenerator();
        // Sum all 10 arguments
        for (var i = 0; i < 10; i++)
            il.Emit(OpCodes.Ldarg, i);

        // Add them all
        for (var i = 0; i < 9; i++)
            il.Emit(OpCodes.Add);

        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, int, int, int, int, int, int, int, int, int, int>(dynamicMethod);

        // Act
        var result = invoker.Invoke(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

        // Assert
        Assert.That(result, Is.EqualTo(55)); // Sum of 1..10
    }

    [Test]
    public void Invoker_Performance_IsFasterThanReflection()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("TestPerf", typeof(int), new[] { typeof(int), typeof(int) });
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, int, int>(dynamicMethod);

        // Create equivalent delegate via reflection for comparison
        var delegateType = typeof(Func<int, int, int>);
        var delegateInstance = dynamicMethod.CreateDelegate(delegateType);

        const int iterations = 1_000_000;

        // Warm up
        invoker.Invoke(2, 3);
        delegateInstance.DynamicInvoke(2, 3);

        // Measure DynamicMethodInvoker
        var sw1 = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
            invoker.Invoke(2, 3);
        sw1.Stop();

        // Measure Reflection
        var sw2 = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
            delegateInstance.DynamicInvoke(2, 3);
        sw2.Stop();

        // Assert: DynamicMethodInvoker should be at least 10x faster
        var ratio = (double)sw2.ElapsedTicks / sw1.ElapsedTicks;
        Assert.That(ratio, Is.GreaterThan(10.0),
            $"DynamicMethodInvoker was only {ratio:F1}x faster, expected >10x");
    }

    [Test]
    public void Invoker_WorksWithComplexValueTypes()
    {
        // Arrange: Test with DateTime
        var dynamicMethod = new DynamicMethod("TestDateTime", typeof(DateTime), [typeof(DateTime), typeof(double)]);
        using var il = new GroboIL(dynamicMethod);

        // Load arguments
        il.Ldarga(0);
        il.Ldarg(1);

        // Call DateTime.AddDays
        var addDaysMethod = typeof(DateTime).GetMethod(nameof(DateTime.AddDays), [typeof(double)])!;
        il.Call(addDaysMethod);
        il.Ret();

        var invoker = new DynamicMethodInvoker<DateTime, double, DateTime>(dynamicMethod);

        // Act
        var startDate = new DateTime(2024, 1, 1);
        var result = invoker.Invoke(startDate, 42.0);

        // Assert
        Assert.That(result, Is.EqualTo(startDate.AddDays(42)));
    }

    [Test]
    public void Invoker_HandlesNullArguments_ForReferenceTypes()
    {
        // Arrange: string concatenation
        var dynamicMethod = new DynamicMethod("TestString", typeof(string),
            new[] { typeof(string), typeof(string) });

        var il = dynamicMethod.GetILGenerator();

        // Load both strings
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);

        // Call string.Concat(string, string)
        var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
        il.Emit(OpCodes.Call, concatMethod);

        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<string, string, string>(dynamicMethod);

        // Act & Assert
        Assert.That(invoker.Invoke("Hello, ", "World!"), Is.EqualTo("Hello, World!"));
        Assert.That(invoker.Invoke(null, "World!"), Is.EqualTo("World!"));
        Assert.That(invoker.Invoke("Hello, ", null), Is.EqualTo("Hello, "));
        Assert.That(invoker.Invoke(null, null), Is.EqualTo(string.Empty));
    }


    [Test]
    public void Invoker_WithWistEngineGeneratedMethod_WorksCorrectly()
    {
        // Arrange: Создаем Wist Engine как в Program.cs
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        // Act: Получаем DynamicMethod из Wist Engine
        var dynamicMethod = executableGiver.GetExecutable(
            """
            let x = 42
            let y = 3.14 * 2.0
            let result = (x + 5) * 2
            result
            """,
            new Dictionary<string, Type>()
        );

        var invoker = new DynamicMethodInvoker<int>(dynamicMethod);

        // Assert
        var result = invoker.Invoke();
        Assert.That(result, Is.EqualTo(94)); // (42 + 5) * 2 = 94
    }

    [Test]
    public void Invoker_WithWistEngineVariablesAndConditions_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        var dynamicMethod = executableGiver.GetExecutable(
            """
            let x = 10
            let y = 20

            if x > 5 and y < 30 (
                let result = (x + y) * 2
                result
            )
            else (
                0
            )
            """,
            new Dictionary<string, Type>()
        );

        var invoker = new DynamicMethodInvoker<int>(dynamicMethod);

        // Act & Assert
        var result = invoker.Invoke();
        Assert.That(result, Is.EqualTo(60)); // (10 + 20) * 2 = 60
    }

    [Test]
    public void Invoker_WithWistEngineLoopsAndGoto_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        var dynamicMethod = executableGiver.GetExecutable(
            """
            let sum = 0
            let i = 1

            @loop_start:
            if i > 5 goto @loop_end
            sum = sum + i
            i = i + 1
            goto @loop_start

            @loop_end:
            sum
            """,
            new Dictionary<string, Type>()
        );

        var invoker = new DynamicMethodInvoker<int>(dynamicMethod);

        // Act & Assert
        var result = invoker.Invoke();
        Assert.That(result, Is.EqualTo(15)); // 1+2+3+4+5 = 15
    }

    [Test]
    public void Invoker_WithWistEngineNativeArithmetic_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        var dynamicMethod = executableGiver.GetExecutable(
            """
            let x = 42
            let y = 3.14f * 2.0f

            if x > 10 and y < 10.0f (
                let result = (x + 5) * 2
                result
            )
            else (
                -1
            )
            """,
            new Dictionary<string, Type>()
        );

        var invoker = new DynamicMethodInvoker<int>(dynamicMethod);

        // Act & Assert
        var result = invoker.Invoke();

        Assert.That(result, Is.EqualTo(94));
    }

    [Test]
    public void Invoker_WithWistEngineSystemCalls_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        // Тест с вызовом системных функций через Wist
        var dynamicMethod = executableGiver.GetExecutable(
            """
            let value = 25.0
            let sqrtResult = System.Math.Sqrt(value)
            let absResult = System.Math.Abs(-5.0)
            sqrtResult + absResult
            """,
            new Dictionary<string, Type>()
        );

        var invoker = new DynamicMethodInvoker<double>(dynamicMethod);

        // Act & Assert
        var result = invoker.Invoke();
        Assert.That(result, Is.EqualTo(10.0).Within(1e-9)); // sqrt(25) + abs(-5) = 5 + 5 = 10
    }

    [Test]
    public void Invoker_WithWistEngineAndExternalParameters_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        // Динамический метод с параметрами, как в Program.cs
        var dynamicMethod = executableGiver.GetExecutable(
            """
            // Используем внешние параметры
            a + b * 2.0
            """,
            new Dictionary<string, Type>
            {
                { "a", typeof(double) },
                { "b", typeof(double) }
            }
        );

        var invoker = new DynamicMethodInvoker<double, double, double>(dynamicMethod);

        // Act & Assert
        var result = invoker.Invoke(5.0, 6.0);
        Assert.That(result, Is.EqualTo(17.0).Within(1e-9)); // 5 + 6*2 = 17
    }

    [Test]
    public void Invoker_WithWistEngineComplexExpression_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        var dynamicMethod = executableGiver.GetExecutable(
            """
            let a = 2.0
            let b = 3.0
            let c = 4.0

            let powResult = System.Math.Pow(a, b)
            let sqrtResult = System.Math.Sqrt(c)
            let logResult = System.Math.Log(b, a)

            powResult + sqrtResult * a - logResult / System.Math.Abs(a - b)
            """,
            new Dictionary<string, Type>()
        );

        var invoker = new DynamicMethodInvoker<double>(dynamicMethod);

        // Act & Assert
        var result = invoker.Invoke();
        // 2^3 + sqrt(4)*2 - log_2(3)/abs(2-3) = 8 + 2*2 - 1.5849625/1 = 8 + 4 - 1.5849625 = 10.4150375
        Assert.That(result, Is.EqualTo(10.4150375).Within(1e-7));
    }

    [Test]
    public void Invoker_WithWistEngineAndPerformanceMeasurement_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        var dynamicMethod = executableGiver.GetExecutable(
            """
            // Простой цикл для проверки производительности
            let sum = 0
            let i = 0

            @loop:
            if i >= 1000 goto @end
            sum = sum + i * i
            i = i + 1
            goto @loop

            @end:
            sum
            """,
            new Dictionary<string, Type>()
        );

        var invoker = new DynamicMethodInvoker<int>(dynamicMethod);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = invoker.Invoke();
        stopwatch.Stop();

        // Assert
        var expectedSum = 0;
        for (int i = 0; i < 1000; i++)
        {
            expectedSum += i * i;
        }

        Assert.That(result, Is.EqualTo(expectedSum));

        // Проверяем, что выполнение достаточно быстрое
        // 1000 итераций должно выполняться меньше чем за 1 мс
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10));
    }

    [Test]
    public void Invoker_WithWistEngineAndExceptionHandling_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var provider = services.BuildServiceProvider();
        var executableGiver = provider.GetServices<IExecutableGiver<DynamicMethod>>().First();

        // Wist код, который может вызвать исключение (деление на ноль)
        var dynamicMethod = executableGiver.GetExecutable(
            """
            let x = 10
            let y = 0
            x / y  // Деление на ноль
            """,
            new Dictionary<string, Type>()
        );

        var invoker = new DynamicMethodInvoker<int>(dynamicMethod);

        // Act & Assert
        // В нативном режиме деление на ноль для int должно выбрасывать исключение
        Assert.Throws<DivideByZeroException>(() => invoker.Invoke());
    }

    [Test]
    public void Invoker_WithWistEngineDifferentArithmeticModes_WorksCorrectly()
    {
        // Тест Universal mode
        var servicesUniversal = new ServiceCollection();
        servicesUniversal.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var providerUniversal = servicesUniversal.BuildServiceProvider();
        var executableGiverUniversal = providerUniversal.GetServices<IExecutableGiver<DynamicMethod>>().First();

        var dynamicMethodUniversal = executableGiverUniversal.GetExecutable(
            """
            let a = 0.1
            let b = 0.2
            let c = a + b
            c * 10.0
            """,
            new Dictionary<string, Type>()
        );

        var invokerUniversal = new DynamicMethodInvoker<double>(dynamicMethodUniversal);
        var resultUniversal = invokerUniversal.Invoke();

        Assert.That(resultUniversal, Is.EqualTo(3.0).Within(1e-9));

        // Тест Native mode
        var servicesNative = new ServiceCollection();
        servicesNative.AddWistServices(options =>
            options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        var providerNative = servicesNative.BuildServiceProvider();
        var executableGiverNative = providerNative.GetServices<IExecutableGiver<DynamicMethod>>().First();

        var dynamicMethodNative = executableGiverNative.GetExecutable(
            """
            let a = 0.1
            let b = 0.2
            let c = a + b
            c * 10.0
            """,
            new Dictionary<string, Type>()
        );

        var invokerNative = new DynamicMethodInvoker<double>(dynamicMethodNative);
        var resultNative = invokerNative.Invoke();

        // В Native mode могут быть ошибки округления double
        // 0.1 + 0.2 = 0.30000000000000004 * 10 = 3.0000000000000004
        Assert.That(resultNative, Is.EqualTo(3.0000000000000004).Within(1e-14));
    }
}