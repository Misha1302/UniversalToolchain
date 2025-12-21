// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com
// ./BasicCilCompiler/BasicCilCompilerImpl.cs

using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;
using ExceptionsManager;
using GrEmit;

namespace BasicCilCompiler;

public class BasicCilCompilerImpl : IExecutor
{
    public object Execute(List<(GroboIL, DynamicMethod)> methods)
    {
        var main = CompileToOneMethod(methods);
        return ExecuteInternal(main);
    }

    private object ExecuteInternal(DynamicMethod main)
    {
        return main.Invoke(null, [])!;
    }

    private string? ExtractLabelName(string methodName)
    {
        const string intrinsicMarker = "_!Intrinsic_";
        var markerIndex = methodName.IndexOf(intrinsicMarker, StringComparison.Ordinal);
        return markerIndex >= 0 ? methodName[(markerIndex + intrinsicMarker.Length)..] : null;
    }

    private DynamicMethod CompileToOneMethod(List<(GroboIL, DynamicMethod)> methods)
    {
        var mainMethod = new DynamicMethod("MainMethod", typeof(object), Type.EmptyTypes);
        var il = new GroboIL(mainMethod);

        // Стек для хранения значений во время выполнения
        var localStack = il.DeclareLocal(typeof(List<object>));

        // Инициализация стека
        il.Newobj(typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        il.Stloc(localStack);

        // Словарь для хранения меток
        var labels = new Dictionary<string, GroboIL.Label>();

        // Предварительная обработка для создания меток
        foreach (var (_, method) in methods)
        {
            var labelName = ExtractLabelName(method.Name);
            if (labelName != null) labels[labelName] = il.DefineLabel(Guid.NewGuid().ToString());
        }

        // Компиляция всех методов
        foreach (var (_, method) in methods)
        {
            var labelName = ExtractLabelName(method.Name).NotNull();

            if (method.Name.StartsWith("Label_!Intrinsic"))
            {
                // Отмечаем метку
                il.MarkLabel(labels[labelName]);
            }
            else if (method.Name.StartsWith("Goto_!Intrinsic"))
            {
                // Безусловный переход
                il.Br(labels[labelName]);
            }
            else if (method.Name.StartsWith("CondFGoto_!Intrinsic"))
            {
                // Условный переход если false

                // Берем значение с вершины стека и сохраняем в локальную переменную
                il.Ldloc(localStack);
                il.Ldc_I4(0);
                il.Call(typeof(List<object>).GetMethod("get_Item")!);
                var conditionValue = il.DeclareLocal(typeof(object));
                il.Stloc(conditionValue);

                // Удаляем значение из стека
                il.Ldloc(localStack);
                il.Ldc_I4(0);
                il.Call(typeof(List<object>).GetMethod("RemoveAt")!);

                // Преобразуем значение к bool
                il.Ldloc(conditionValue);

                // Создаем метки для преобразования
                var isBoolLabel = il.DefineLabel(Guid.NewGuid().ToString());
                var endConvertLabel = il.DefineLabel(Guid.NewGuid().ToString());

                // Проверяем, является ли значение bool
                il.Dup();
                il.Isinst(typeof(bool));
                il.Brtrue(isBoolLabel);

                // Если не bool, преобразуем через Convert.ToBoolean
                il.Call(typeof(Convert).GetMethod("ToBoolean", [typeof(object)])!);
                il.Br(endConvertLabel);

                // Если bool, распаковываем
                il.MarkLabel(isBoolLabel);
                il.Unbox_Any(typeof(bool));

                il.MarkLabel(endConvertLabel);

                // Теперь на стеке bool, используем для условного перехода
                il.Brfalse(labels[labelName]);
            }
            else
            {
                // Обычный метод - вызываем его
                CompileMethodCall(il, localStack, method);
            }
        }

        // Возвращаем результат с вершины стека (если есть)
        il.Ldloc(localStack);
        il.Call(typeof(List<object>).GetProperty("Count")!.GetGetMethod()!);
        il.Ldc_I4(0);
        var hasResult = il.DefineLabel(Guid.NewGuid().ToString());
        il.Bgt(hasResult, false);

        // Если стек пуст, возвращаем null
        il.Ldnull();
        il.Ret();

        il.MarkLabel(hasResult);
        il.Ldloc(localStack);
        il.Ldc_I4(0);
        il.Call(typeof(List<object>).GetMethod("get_Item")!);
        il.Ret();

        return mainMethod;
    }

    private void CompileMethodCall(GroboIL il, GroboIL.Local localStack, DynamicMethod method)
    {
        var parameters = method.GetParameters();
        var paramCount = parameters.Length;

        // Временные локальные переменные для хранения параметров
        var paramLocals = new GroboIL.Local[paramCount];

        // Сохраняем параметры из стека во временные переменные
        for (var i = 0; i < paramCount; i++)
        {
            il.Ldloc(localStack);
            il.Ldc_I4(i);
            il.Call(typeof(List<object>).GetMethod("get_Item")!);
            paramLocals[i] = il.DeclareLocal(typeof(object));
            il.Stloc(paramLocals[i]);
        }

        // Загружаем параметры в правильном порядке и преобразуем типы
        for (var i = 0; i < paramCount; i++)
        {
            il.Ldloc(paramLocals[i]);
            var paramType = parameters[i].ParameterType;

            if (paramType.IsValueType)
                il.Unbox_Any(paramType);
            else if (paramType != typeof(object)) il.Castclass(paramType);
        }

        // Вызываем метод
        if (method.IsStatic)
        {
            il.Call(method);
        }
        else
        {
            il.Ldnull();
            il.Call(method);
        }

        // Обрабатываем возвращаемое значение
        if (method.ReturnType != typeof(void))
        {
            // Упаковываем value types
            if (method.ReturnType.IsValueType) il.Box(method.ReturnType);

            // Сохраняем результат во временную переменную
            var resultLocal = il.DeclareLocal(typeof(object));
            il.Stloc(resultLocal);

            // Удаляем использованные аргументы из стека
            if (paramCount > 0)
            {
                il.Ldloc(localStack);
                il.Ldc_I4(0);
                il.Ldc_I4(paramCount);
                il.Call(typeof(List<object>).GetMethod("RemoveRange")!);
            }

            // Добавляем результат в начало стека
            il.Ldloc(localStack);
            il.Ldc_I4(0);
            il.Ldloc(resultLocal);
            il.Call(typeof(List<object>).GetMethod("Insert")!);
        }
        else
        {
            // Для void методов просто удаляем аргументы
            if (paramCount > 0)
            {
                il.Ldloc(localStack);
                il.Ldc_I4(0);
                il.Ldc_I4(paramCount);
                il.Call(typeof(List<object>).GetMethod("RemoveRange")!);
            }
        }
    }
}