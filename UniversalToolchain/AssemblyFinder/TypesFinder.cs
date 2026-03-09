using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using ExceptionsManager;

namespace AssemblyFinder;

public static class TypesFinder
{
    private static readonly object _syncLock = new();
    private static readonly HashSet<Assembly> _loadedAssemblies = new();
    private static readonly Dictionary<string, Assembly> _assemblyCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _badAssemblies = new(StringComparer.OrdinalIgnoreCase);

    // Публичные свойства с ленивой инициализацией
    private static readonly Lazy<IReadOnlyList<Assembly>> _allAssemblies = new(() => LoadAllAssemblies(), true);
    private static readonly Lazy<IReadOnlyList<Type>> _allTypes = new(LoadAllTypes, true);

    static TypesFinder()
    {
        Initialize();
    }

    public static IEnumerable<Assembly> Assemblies => _allAssemblies.Value;
    public static IEnumerable<Type> AllTypes => _allTypes.Value;

    private static void Initialize()
    {
        lock (_syncLock)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsValidAssembly(assembly))
                {
                    _loadedAssemblies.Add(assembly);
                    CacheAssembly(assembly);
                }
            }
        }
    }

    public static Type GetType(string name)
    {
        var type = AllTypes.FirstOrDefault(x => x.FullName == name);
        if (type == null)
            Thrower.InvalidOpEx($"Type '{name}' was not found among loaded assemblies.");
        return type;
    }

    private static bool IsValidAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic) return false;

        try
        {
            // Быстрая проверка - пытаемся получить минимальную информацию
            _ = assembly.GetName();
            // Полная проверка - загрузка типов (но только если еще не проверяли)
            if (!_badAssemblies.Contains(assembly.FullName ?? ""))
                _ = assembly.GetExportedTypes();
            return true;
        }
        catch (BadImageFormatException)
        {
            // Битая сборка
            if (assembly.FullName != null)
                _badAssemblies.Add(assembly.FullName);
            return false;
        }
        catch (FileLoadException)
        {
            // Защищенная или недоступная сборка
            if (assembly.FullName != null)
                _badAssemblies.Add(assembly.FullName);
            return false;
        }
        catch (ReflectionTypeLoadException)
        {
            // Частично загруженная сборка
            return true; // Все еще может содержать полезные типы
        }
        catch
        {
            // Любая другая ошибка
            return false;
        }
    }

    private static bool TryLoadAssembly(string path, [NotNullWhen(true)] out Assembly? assembly)
    {
        assembly = null;

        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(path);
            return TryLoadAssembly(assemblyName, path, out assembly);
        }
        catch (BadImageFormatException)
        {
            // Битый файл
            _badAssemblies.Add(Path.GetFileName(path));
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Нет прав доступа
            return false;
        }
        catch (IOException)
        {
            // Файл занят или недоступен
            return false;
        }
    }

    private static bool TryLoadAssembly(AssemblyName assemblyName, string? path, [NotNullWhen(true)] out Assembly? assembly)
    {
        assembly = null;
        var fullName = assemblyName.FullName;

        lock (_syncLock)
        {
            // Проверяем кэш
            if (_assemblyCache.TryGetValue(fullName, out assembly))
                return true;

            // Проверяем плохие сборки
            if (_badAssemblies.Contains(fullName))
                return false;

            try
            {
                // Загружаем сборку
                assembly = path != null
                    ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path)
                    : Assembly.Load(assemblyName);

                // Валидируем сборку
                if (!IsValidAssembly(assembly))
                {
                    _badAssemblies.Add(fullName);
                    assembly = null;
                    return false;
                }

                // Кэшируем успешную загрузку
                CacheAssembly(assembly);
                _loadedAssemblies.Add(assembly);

                return true;
            }
            catch (BadImageFormatException)
            {
                _badAssemblies.Add(fullName);
                return false;
            }
            catch (FileLoadException ex) when (ex.Message.Contains("administrator") || ex.Message.Contains("elevated"))
            {
                // Требуются повышенные права
                _badAssemblies.Add(fullName);
                return false;
            }
            catch (Exception ex) when (ex is FileNotFoundException or
                                           DirectoryNotFoundException or
                                           UnauthorizedAccessException or
                                           PathTooLongException)
            {
                // Другие ошибки файловой системы
                _badAssemblies.Add(fullName);
                return false;
            }
        }
    }

    private static void CacheAssembly(Assembly assembly)
    {
        var fullName = assembly.FullName;
        if (fullName != null && !_assemblyCache.ContainsKey(fullName))
            _assemblyCache[fullName] = assembly;
    }

    private static void LoadDependencies(Assembly assembly)
    {
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            // Игнорируем системные сборки
            if (reference.Name?.StartsWith("System.") == true ||
                reference.Name?.StartsWith("Microsoft.") == true ||
                reference.Name == "netstandard" ||
                reference.Name == "mscorlib")
                continue;

            lock (_syncLock)
            {
                if (!_assemblyCache.ContainsKey(reference.FullName))
                    TryLoadAssembly(reference, null, out _);
            }
        }
    }

    public static IReadOnlyList<Assembly> LoadAllAssemblies(string? path = null)
    {
        var assemblies = new List<Assembly>();

        // Загружаем сборки из текущего домена
        lock (_syncLock)
        {
            assemblies.AddRange(_loadedAssemblies);
        }

        // Загружаем сборки из текущей директории и поддиректорий
        var currentPath = path ?? AppDomain.CurrentDomain.BaseDirectory;
        var dlls = Directory.EnumerateFiles(currentPath, "*.dll", SearchOption.AllDirectories);

        foreach (var dllPath in dlls)
        {
            if (TryLoadAssembly(dllPath, out var assembly))
            {
                assemblies.Add(assembly);
                // Загружаем зависимости
                LoadDependencies(assembly);
            }
        }

        // Убираем дубликаты (на всякий случай)
        var distinctAssemblies = new HashSet<Assembly>(assemblies);

        return distinctAssemblies.ToArray();
    }

    private static IReadOnlyList<Type> LoadAllTypes()
    {
        var types = new List<Type>();

        foreach (var assembly in Assemblies)
        {
            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Частично загруженные типы
                var loadedTypes = ex.Types.Where(t => t != null);
                types.AddRange(loadedTypes!);
            }
            catch
            {
                // Пропускаем сборки, которые не могут загрузить типы
            }
        }

        return types.Distinct().ToArray();
    }

    // Метод для добавления сборок вручную (например, для тестирования)
    public static void RegisterAssembly(Assembly assembly)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (assembly == null)
            Thrower.ArgumentNull(nameof(assembly));

        lock (_syncLock)
        {
            if (!IsValidAssembly(assembly))
                return;

            _loadedAssemblies.Add(assembly);
            CacheAssembly(assembly);
            LoadDependencies(assembly);
        }
    }
}