namespace Tests.Stress;

[TestFixture]
[Category("Concurrency")]
[Parallelizable(ParallelScope.None)] // These tests must not run in parallel.
public class DynamicMethodInvokerConcurrencyTests
{
    private const int ThreadCount = 8;
    private const int IterationsPerThread = 10000;
    private const int TimeoutMs = 30000;

    [Test]
    [CancelAfter(TimeoutMs)]
    public void Invoker_ConcurrentCalls_NoRaceConditions()
    {
        // Arrange: create a thread-safe counter through DynamicMethod.
        var dynamicMethod = new DynamicMethod("ConcurrentCounter", typeof(int),
            new[] { typeof(int), typeof(int) });

        var il = dynamicMethod.GetILGenerator();
        // Simulate an atomic operation: (a + b) * 2.
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, int, int>(dynamicMethod);

        // Act: run multiple threads.
        var results = new ConcurrentBag<int>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(ThreadCount);

        var threads = Enumerable.Range(0, ThreadCount).Select(threadId => new Thread(() =>
        {
            try
            {
                barrier.SignalAndWait(); // Synchronize the start of all threads.

                for (var i = 0; i < IterationsPerThread; i++)
                {
                    // Each thread computes a unique value.
                    var result = invoker.Invoke(threadId, i);
                    results.Add(result);

                    // Small delays increase the chance of thread switching.
                    if (i % 100 == 0)
                        Thread.Yield();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        // Start all threads.
        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert.
        Assert.That(exceptions, Is.Empty, "Exceptions must not escape worker threads.");
        Assert.That(results.Count, Is.EqualTo(ThreadCount * IterationsPerThread));

        // Verify that all results are correct.
        var expectedResults = Enumerable.Range(0, ThreadCount)
            .SelectMany(threadId => Enumerable.Range(0, IterationsPerThread)
                .Select(i => (threadId + i) * 2));

        Assert.That(results, Is.EquivalentTo(expectedResults));
    }

    [Test]
    [CancelAfter(TimeoutMs)]
    public void Invoker_ParallelFor_ThreadSafety()
    {
        // Arrange: dynamic method with state carried through parameters.
        var dynamicMethod = new DynamicMethod("ParallelAccumulator", typeof(long),
            new[] { typeof(long), typeof(long) });

        var il = dynamicMethod.GetILGenerator();
        // Аккумулирующая операция: a + b^2
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<long, long, long>(dynamicMethod);

        // Act: Используем Parallel.For для конкурентных вызовов
        long totalSum = 0;
        var lockObj = new object();

        Parallel.For(0, ThreadCount * IterationsPerThread, i =>
        {
            // Каждая итерация независима, но мы аккумулируем результат
            var result = invoker.Invoke(i, i % 100);

            lock (lockObj)
            {
                totalSum += result;
            }
        });

        // Assert: Проверяем корректность суммы
        long expectedSum = 0;
        for (long i = 0; i < ThreadCount * IterationsPerThread; i++)
            expectedSum += i + i % 100 * (i % 100);

        Assert.That(totalSum, Is.EqualTo(expectedSum));
    }

    [Test]
    [CancelAfter(TimeoutMs)]
    public void Invoker_MultipleInstances_ConcurrentCreationAndExecution()
    {
        // Arrange: Создаем несколько разных динамических методов
        var methods = Enumerable.Range(0, 4).Select(i =>
        {
            var dm = new DynamicMethod($"Method_{i}", typeof(int), new[] { typeof(int) });
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, i + 1); // Множитель 1, 2, 3, 4
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Ret);
            return dm;
        }).ToArray();

        // Act: Потоки создают инвокеры и выполняют их конкурентно
        var exceptions = new ConcurrentBag<Exception>();

        var threads = Enumerable.Range(0, ThreadCount).Select(threadId => new Thread(() =>
        {
            try
            {
                var random = new Random(threadId);
                for (var i = 0; i < IterationsPerThread; i++)
                {
                    // Случайно выбираем метод
                    var methodIndex = random.Next(methods.Length);
                    var invokerType = typeof(DynamicMethodInvoker<,>).MakeGenericType(typeof(int), typeof(int));
                    var invoker = Activator.CreateInstance(invokerType, methods[methodIndex]);

                    // Вызываем через reflection, так как тип инвокера разный
                    var invokeMethod = invokerType.GetMethod("Invoke");
                    Assert.That(invokeMethod, Is.Not.Null);
                    var resultObj = invokeMethod!.Invoke(invoker, new object[] { threadId * 1000 + i });
                    Assert.That(resultObj, Is.TypeOf<int>());
                    var result = (int)resultObj;

                    // Правильный ожидаемый результат
                    var expected = (threadId * 1000 + i) * (methodIndex + 1);
                    if (result != expected)
                        Thrower.InvalidOpEx($"Некорректный результат конкурентного вызова: {result}, ожидалось: {expected}.");
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        // Запускаем потоки
        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.That(exceptions, Is.Empty, "Исключения в потоках");
    }

    [Test]
    [CancelAfter(TimeoutMs)]
    public void Invoker_SharedInstance_ConcurrentCallsWithMemoryBarriers_Simple()
    {
        // Arrange: Простой метод без ref параметров
        var dynamicMethod = new DynamicMethod("AtomicIncrement", typeof(long), Type.EmptyTypes);

        using (var il = new GroboIL(dynamicMethod))
        {
            // Загружаем статическое поле
            il.Ldflda(typeof(TestClass).GetField("_sharedValue",
                BindingFlags.Static | BindingFlags.NonPublic));
            // Загружаем 1
            il.Ldc_I8(1);
            // Атомарно добавляем
            il.Call(typeof(Interlocked).GetMethod("Add",
                [typeof(long).MakeByRefType(), typeof(long)]));
            // Возвращаем результат
            il.Ret();
        }

        var invoker = new DynamicMethodInvoker<object[], long, long>(dynamicMethod);

        // Статическая переменная для всех потоков
        TestClass.ResetSharedValue();

        // Act
        var exceptions = new ConcurrentBag<Exception>();
        var spinWait = new SpinWait();

        var threads = Enumerable.Range(0, ThreadCount).Select(_ => new Thread(() =>
        {
            try
            {
                for (var i = 0; i < IterationsPerThread; i++)
                {
                    invoker.Invoke(new object[0], 1);

                    if (i % 100 == 0)
                        spinWait.SpinOnce();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.That(exceptions, Is.Empty);
        Assert.That(TestClass.SharedValue, Is.EqualTo((long)ThreadCount * IterationsPerThread));
    }

    // Вспомогательный класс
    private static class TestClass
    {
        private static long _sharedValue;

        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public static long SharedValue => _sharedValue;

        public static void ResetSharedValue() => _sharedValue = 0;
    }

    [Test]
    [CancelAfter(TimeoutMs)]
    public void Invoker_ThreadPool_QueueUserWorkItem()
    {
        // Arrange: Простой метод для выполнения в пуле потоков
        var dynamicMethod = new DynamicMethod("ThreadPoolTest", typeof(int),
            new[] { typeof(int), typeof(int) });

        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Div);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, int, int>(dynamicMethod);

        // Act: Используем ThreadPool для асинхронных вызовов
        var completionEvents = new ManualResetEvent[ThreadCount];
        var results = new int[ThreadCount];
        var exceptions = new ConcurrentBag<Exception>();

        for (var i = 0; i < ThreadCount; i++)
        {
            completionEvents[i] = new ManualResetEvent(false);
            var threadId = i;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var result = invoker.Invoke(threadId * 100, threadId + 1);
                    results[threadId] = result;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    completionEvents[threadId].Set();
                }
            });
        }

        // Ждем завершения всех задач
        WaitHandle.WaitAll(completionEvents.Cast<WaitHandle>().ToArray(), TimeoutMs);

        // Assert
        Assert.That(exceptions, Is.Empty, "Исключения в пуле потоков");

        for (var i = 0; i < ThreadCount; i++)
        {
            var expected = i * 100 * (i + 1) / 2;
            Assert.That(results[i], Is.EqualTo(expected), $"Некорректный результат для потока {i}");
        }
    }

    [Test]
    [CancelAfter(TimeoutMs)]
    public void Invoker_TaskParallelLibrary_AsyncAwaitPattern()
    {
        // Arrange: Асинхронный метод
        var dynamicMethod = new DynamicMethod("AsyncTest", typeof(double),
            new[] { typeof(double), typeof(int) });

        var il = dynamicMethod.GetILGenerator();
        // Вычисление: a * sqrt(b)
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Conv_R8);

        var sqrtMethod = typeof(Math).GetMethod("Sqrt", new[] { typeof(double) });
        Assert.That(sqrtMethod, Is.Not.Null);
        il.Emit(OpCodes.Call, sqrtMethod);

        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<double, int, double>(dynamicMethod);

        // Act: Создаем задачи через TPL
        var tasks = Enumerable.Range(0, ThreadCount).Select(async threadId =>
        {
            double sum = 0;
            for (var i = 0; i < IterationsPerThread; i++)
            {
                // Асинхронное выполнение
                var result = await Task.Run(() => invoker.Invoke(threadId * 1.5, i + 1));
                sum += result;

                // Имитация асинхронной работы
                if (i % 100 == 0)
                    await Task.Yield();
            }
            return sum;
        }).ToArray();

        // Ждем завершения всех задач
        var taskResults = Task.WhenAll(tasks).GetAwaiter().GetResult();

        // Assert
        Assert.That(taskResults.Length, Is.EqualTo(ThreadCount));

        for (var threadId = 0; threadId < ThreadCount; threadId++)
        {
            // Проверяем корректность вычислений
            double expectedSum = 0;
            for (var i = 0; i < IterationsPerThread; i++)
                expectedSum += threadId * 1.5 * Math.Sqrt(i + 1);

            Assert.That(taskResults[threadId], Is.EqualTo(expectedSum).Within(1e-9),
                $"Некорректная сумма для потока {threadId}");
        }
    }

    [Test]
    [CancelAfter(TimeoutMs)]
    public void Invoker_ProducerConsumerPattern_ConcurrentQueue()
    {
        // Arrange: Метод для обработки элементов
        var dynamicMethod = new DynamicMethod("ProcessItem", typeof(string),
            new[] { typeof(int), typeof(string) });

        var il = dynamicMethod.GetILGenerator();
        // Конкатенация: $"{prefix}_{value * 2}"
        il.Emit(OpCodes.Ldarg_1); // prefix
        il.Emit(OpCodes.Ldstr, "_");
        var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
        Assert.That(concatMethod, Is.Not.Null);
        il.Emit(OpCodes.Call, concatMethod);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Box, typeof(int));
        var toStringMethod = typeof(object).GetMethod("ToString");
        Assert.That(toStringMethod, Is.Not.Null);
        il.Emit(OpCodes.Callvirt, toStringMethod);

        il.Emit(OpCodes.Call, concatMethod);
        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<int, string, string>(dynamicMethod);

        // Act: Producer-Consumer паттерн
        var queue = new ConcurrentQueue<int>();
        var results = new ConcurrentBag<string>();
        var exceptions = new ConcurrentBag<Exception>();

        // Producer потоки
        var producerThreads = Enumerable.Range(0, ThreadCount / 2).Select(threadId => new Thread(() =>
        {
            try
            {
                for (var i = 0; i < IterationsPerThread; i++)
                {
                    queue.Enqueue(threadId * 1000 + i);
                    Thread.Sleep(0); // Yield
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        // Consumer потоки
        var consumerThreads = Enumerable.Range(0, ThreadCount / 2).Select(threadId => new Thread(() =>
        {
            try
            {
                while (queue.TryDequeue(out var item) || producerThreads.Any(t => t.IsAlive))
                {
                    if (queue.TryDequeue(out item))
                    {
                        var result = invoker.Invoke(item, $"Thread{threadId}");
                        results.Add(result);
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        // Запускаем producers
        foreach (var thread in producerThreads) thread.Start();
        Thread.Sleep(10); // Даем producers немного поработать

        // Запускаем consumers
        foreach (var thread in consumerThreads) thread.Start();

        // Ждем завершения
        foreach (var thread in producerThreads) thread.Join();
        foreach (var thread in consumerThreads) thread.Join();

        // Assert
        Assert.That(exceptions, Is.Empty, "Исключения в producer/consumer потоках");
        Assert.That(results, Is.Not.Empty);

        // Проверяем формат результатов
        foreach (var result in results)
        {
            Assert.That(result, Does.StartWith("Thread"));
            Assert.That(result, Does.Contain("_"));
        }
    }

    [Test]
    [CancelAfter(TimeoutMs)]
    [Category("Stress")]
    public void Invoker_StressTest_HeavyConcurrency()
    {
        // Arrange: Стресс-тест с большим количеством потоков
        var stressThreadCount = Environment.ProcessorCount * 4;
        var stressIterations = 5000;

        var dynamicMethod = new DynamicMethod("StressOperation", typeof(long),
            new[] { typeof(long), typeof(long), typeof(long) });

        var il = dynamicMethod.GetILGenerator();
        // Сложная операция: (a * b) / (c + 1) + a % b
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Mul);

        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Conv_I8);

        il.Emit(OpCodes.Div);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Rem);
        il.Emit(OpCodes.Add);

        il.Emit(OpCodes.Ret);

        var invoker = new DynamicMethodInvoker<long, long, long, long>(dynamicMethod);

        // Act: Запускаем множество потоков
        long totalOperations = 0;
        var exceptions = new ConcurrentBag<Exception>();
        var spinWait = new SpinWait();

        var threads = Enumerable.Range(0, stressThreadCount).Select(threadId => new Thread(() =>
        {
            try
            {
                long localCount = 0;
                var random = new Random(threadId);

                for (var i = 0; i < stressIterations; i++)
                {
                    var a = random.Next(1, 1000);
                    var b = random.Next(1, 100);
                    var c = random.Next(1, 50);

                    invoker.Invoke(a, b, c);
                    localCount++;

                    // Частая смена контекста
                    if (i % 10 == 0)
                        spinWait.SpinOnce();
                }

                Interlocked.Add(ref totalOperations, localCount);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        // Запускаем все потоки
        var sw = Stopwatch.StartNew();
        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();
        sw.Stop();

        // Assert
        Assert.That(exceptions, Is.Empty, "Исключения в стресс-тесте");
        Assert.That(totalOperations, Is.EqualTo(stressThreadCount * stressIterations));

        Console.WriteLine($"Стресс-тест: {stressThreadCount} потоков, {stressIterations} итераций, время: {sw.ElapsedMilliseconds}ms");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(TimeoutMs), "Стресс-тест превысил таймаут");
    }

    [Test]
    [CancelAfter(TimeoutMs)]
    public void Invoker_DeadlockDetection_SafeReentrancy()
    {
        // Arrange: Метод, который может вызывать себя рекурсивно
        var dynamicMethod = new DynamicMethod("RecursiveSafe", typeof(int),
            new[] { typeof(int), typeof(int) });

        using var il = new GroboIL(dynamicMethod);

        var endLabel = il.DefineLabel(Guid.NewGuid().ToString());

        // Базовый случай: если depth <= 0, возвращаем value
        il.Ldarg(1);
        il.Ldc_I4(0);
        il.Ble(endLabel, false);

        // Рекурсивный случай: value + recursive(value, depth-1)
        il.Ldarg(0);
        il.Ldarg(0);
        il.Ldarg(1);
        il.Ldc_I4(1);
        il.Sub();

        // Рекурсивный вызов (имитация)
        il.Add();
        il.Add();
        il.Ret();

        il.MarkLabel(endLabel);
        il.Ldarg(0);
        il.Ret();

        var invoker = new DynamicMethodInvoker<int, int, int>(dynamicMethod);

        // Act: Пытаемся создать потенциальный deadlock через мониторы
        var lockObject = new object();
        var deadlockDetected = false;
        var exceptions = new ConcurrentBag<Exception>();

        var threads = Enumerable.Range(0, 2).Select(threadId => new Thread(() =>
        {
            try
            {
                if (Monitor.TryEnter(lockObject, 1000))
                    try
                    {
                        // Выполняем вызов под lock'ом
                        invoker.Invoke(threadId * 10, 5);

                        // Другой поток попытается взять тот же lock
                        Thread.Sleep(100);
                    }
                    finally
                    {
                        Monitor.Exit(lockObject);
                    }
                else
                    deadlockDetected = true;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        // Запускаем с небольшой задержкой между потоками
        threads[0].Start();
        Thread.Sleep(50);
        threads[1].Start();

        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.That(exceptions, Is.Empty, "Исключения в тесте на deadlock");
        Assert.That(deadlockDetected, Is.False, "Обнаружен потенциальный deadlock");
    }
}
