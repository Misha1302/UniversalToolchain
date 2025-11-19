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

    private DynamicMethod CompileToOneMethod(List<(GroboIL, DynamicMethod)> methods)
    {
        var method = new DynamicMethod("main", typeof(object), []);
        var il = new GroboIL(method);

        // SSA: Создаем словарь для хранения SSA переменных
        var ssaVariables = new Dictionary<string, GroboIL.Local>();
        var ssaCounter = new Dictionary<string, int>();

        // Словарь для меток
        var labels = new Dictionary<string, GroboIL.Label>();

        // Функция для получения SSA имени переменной
        string GetSSAName(string baseName)
        {
            if (!ssaCounter.ContainsKey(baseName))
            {
                ssaCounter[baseName] = 0;
                return $"{baseName}_0";
            }

            ssaCounter[baseName]++;
            return $"{baseName}_{ssaCounter[baseName]}";
        }

        // Функция для создания SSA переменной
        GroboIL.Local CreateSSAVariable(string baseName, Type type)
        {
            var ssaName = GetSSAName(baseName);
            var local = il.DeclareLocal(type);
            ssaVariables[ssaName] = local;
            return local;
        }

        // Функция для получения текущей SSA переменной
        GroboIL.Local GetCurrentSSAVariable(string baseName)
        {
            var currentVersion = ssaCounter.ContainsKey(baseName) ? ssaCounter[baseName] : 0;
            var ssaName = $"{baseName}_{currentVersion}";

            if (!ssaVariables.ContainsKey(ssaName))
                throw new InvalidOperationException($"SSA variable {ssaName} not found");

            return ssaVariables[ssaName];
        }

        // Обратная передача аргументов: стек для хранения аргументов в обратном порядке
        var reverseArgsStack = new Stack<GroboIL.Local>();

        foreach (var m in methods)
        {
            var name = m.Item2.Name;

            if (!name.Contains("!Intrinsic"))
            {
                // Обычный метод - обрабатываем с обратной передачей аргументов
                var parameters = m.Item2.GetParameters();
                var argsCount = parameters.Length;

                // Помещаем аргументы в обратном порядке
                var args = new GroboIL.Local[argsCount];
                for (var i = 0; i < argsCount; i++)
                {
                    if (reverseArgsStack.Count == 0)
                        Thrower.InvalidOpEx("Not enough arguments on reverse stack");

                    args[i] = reverseArgsStack.Pop();
                }

                // Загружаем аргументы в обратном порядке
                foreach (var arg in args) il.Ldloc(arg);

                il.Call(m.Item2);

                // Сохраняем результат в SSA переменную
                if (m.Item2.ReturnType != typeof(void))
                {
                    var resultVar = CreateSSAVariable("result", m.Item2.ReturnType);
                    il.Stloc(resultVar);
                    reverseArgsStack.Push(resultVar); // Помещаем результат в стек для обратной передачи
                }
            }
            else if (name.Contains("Goto_!Intrinsic"))
            {
                var labelName = name[(name.LastIndexOf('_') + 1)..];
                if (!labels.ContainsKey(labelName))
                    labels[labelName] = il.DefineLabel(labelName);
                il.Br(labels[labelName]);
            }
            else if (name.Contains("Label_!Intrinsic"))
            {
                var labelName = name[(name.LastIndexOf('_') + 1)..];
                if (!labels.ContainsKey(labelName))
                    labels[labelName] = il.DefineLabel(labelName);
                il.MarkLabel(labels[labelName]);

                // SSA: При входе в блок создаем новые версии переменных
                var varsToUpdate = ssaVariables.Keys
                    .Where(k => !k.EndsWith("_0")) // Сохраняем только начальные версии
                    .ToList();

                foreach (var varKey in varsToUpdate)
                {
                    var baseName = varKey.Substring(0, varKey.LastIndexOf('_'));
                    var newVar = CreateSSAVariable(baseName, ssaVariables[varKey].Type);

                    // φ-функция: копируем значение из предыдущей версии
                    il.Ldloc(ssaVariables[varKey]);
                    il.Stloc(newVar);
                }
            }
            else if (name.Contains("CondFGoto_!Intrinsic"))
            {
                var labelName = name[(name.LastIndexOf('_') + 1)..];
                if (!labels.ContainsKey(labelName))
                    labels[labelName] = il.DefineLabel(labelName);

                // Берем условие из стека обратных аргументов
                if (reverseArgsStack.Count == 0)
                    Thrower.InvalidOpEx("No condition on reverse stack for conditional goto");

                var conditionVar = reverseArgsStack.Pop();
                il.Ldloc(conditionVar);
                il.Brfalse(labels[labelName]);
            }
            else
            {
                Thrower.InvalidOpEx($"Unknown method {name}");
            }
        }

        // Возвращаем результат с вершины стека обратных аргументов
        if (reverseArgsStack.Count > 0)
        {
            var loc = reverseArgsStack.Pop();
            il.Ldloc(loc);
            if (loc.Type.IsValueType) il.Box(loc.Type);
        }
        else
            il.Ldnull();

        il.Ret();
        return method;
    }
}