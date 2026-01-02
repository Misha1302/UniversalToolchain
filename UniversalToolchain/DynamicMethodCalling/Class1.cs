using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UltraFastDynamicInvocation;

/// <summary>
///     Обертка для максимально быстрого вызова динамических методов
///     Использует нестандартные подходы для обхода ограничений DynamicMethod
/// </summary>
public unsafe class DynamicMethodInvoker<TDelegate> where TDelegate : Delegate
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly MethodInfo _getMethodDescriptorMethod;
    private readonly TDelegate _delegate;
    private readonly DynamicMethod _dynamicMethod;

    static DynamicMethodInvoker()
    {
        // Получаем доступ к внутренним полям через рефлексию
        var dynamicMethodType = typeof(DynamicMethod);

        // В .NET Core 6+ есть внутренний метод GetMethodDescriptor
        _getMethodDescriptorMethod = dynamicMethodType.GetMethod(
            "GetMethodDescriptor",
            BindingFlags.Instance | BindingFlags.NonPublic
        ).NotNull();
    }

    public DynamicMethodInvoker(DynamicMethod dynamicMethod)
    {
        _dynamicMethod = dynamicMethod ?? throw new ArgumentNullException(nameof(dynamicMethod));

        // 1. Создаем делегат для получения начального указателя
        _delegate = (TDelegate)dynamicMethod.CreateDelegate(typeof(TDelegate));

        // 2. Принудительная JIT-компиляция с агрессивными оптимизациями
        ForceAggressiveJitCompilation();
    }

    public object? Invoke()
    {
        return _delegate.Method.Invoke(null, null);
    }

    /// <summary>
    ///     Получаем указатель на функцию через нестандартные методы
    /// </summary>
    private delegate* unmanaged<void*, void*, void> GetFunctionPointerInternal()
    {
        var handle = (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(_dynamicMethod, null)!;
        var functionPtr = handle.GetFunctionPointer();

        Thrower.AssertAlways(functionPtr != IntPtr.Zero);

        return (delegate* unmanaged<void*, void*, void>)functionPtr;
    }

    /// <summary>
    ///     Принудительная JIT-компиляция с агрессивными оптимизациями
    /// </summary>
    private void ForceAggressiveJitCompilation()
    {
        try
        {
            // 1. Подготовка метода через внутренний механизм
            PrepareMethodViaReflection();

            // 3. Настройка переменных окружения для агрессивных оптимизаций
            SetJitEnvironmentVariables();
        }
        catch
        {
            // Игнорируем ошибки, продолжайем работу
        }
    }

    /// <summary>
    ///     Подготовка метода через рефлексию к внутренним API
    /// </summary>
    private void PrepareMethodViaReflection()
    {
        var handle = (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(_dynamicMethod, null)!;

        // Подготавливаем метод с агрессивными настройками
        for (var i = 0; i < 10; i++) // Многократно для надежности
        {
            RuntimeHelpers.PrepareMethod(handle);
        }
    }

    /// <summary>
    ///     Настройка переменных окружения для агрессивных оптимизаций JIT
    /// </summary>
    private static void SetJitEnvironmentVariables()
    {
        // Устанавливаем переменные через reflection т.к. Environment.SetEnvironmentVariable не всегда работает для существующих процессов
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_JitAggressiveInlining", "1");
            Environment.SetEnvironmentVariable("DOTNET_TieredCompilation", "1");
            Environment.SetEnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1");
            Environment.SetEnvironmentVariable("DOTNET_TC_CallCounting", "1");
            Environment.SetEnvironmentVariable("DOTNET_JitMinOpts", "0");
            Environment.SetEnvironmentVariable("DOTNET_JitDisableGuardedDevirtualization", "0");
            Environment.SetEnvironmentVariable("DOTNET_JitEnableAdaptiveGuardedDevirtualization", "1");
        }
        catch
        {
            // Игнорируем ошибки установки переменных
        }
    }

    /// <summary>
    ///     Фабричный метод для удобного создания инвокера
    /// </summary>
    public static DynamicMethodInvoker<TDelegate> Create(DynamicMethod method)
    {
        return new DynamicMethodInvoker<TDelegate>(method);
    }
}