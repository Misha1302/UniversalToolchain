using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using HostOnlyContractFixture;
using SafeMathFunctionsModule;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Functions.Abstractions;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.FreshProcessHost;

internal static class Program
{
    private const string RuntimeFixtureAssemblyName = "RuntimeHostileFixture";
    private const string UnregisteredDependencyFixtureAssemblyName = "RuntimeUnregisteredDependencyFixture";

    public static int Main(string[] args)
    {
        try
        {
            return args.Length > 0
                ? args[0] switch
                {
                    "hostile" => RunHostilePreload(args),
                    "unregistered-default-fallback" => RunUnregisteredDefaultFallback(args),
                    _ => RunWistScenario(args)
                }
                : RunWistScenario(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int RunWistScenario(IReadOnlyList<string> args)
    {
        var scenario = args.Count == 0 ? "none" : args[0];
        ApplyPreloadScenario(scenario);
        return ExecuteWistBoundaryChecks();
    }

    private static void ApplyPreloadScenario(string scenario)
    {
        switch (scenario)
        {
            case "contract-first":
                LoadDefaultFromOutput("UniversalToolchain.Functions.Abstractions.dll");
                break;
            case "unrelated-first":
                LoadDefaultFromOutput("ExceptionsManager.dll");
                LoadDefaultFromOutput("UniversalToolchain.Functions.Abstractions.dll");
                break;
            case "none":
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown preload scenario.");
        }
    }

    private static void LoadDefaultFromOutput(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, fileName));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Preload assembly was not copied to the fresh-process output: '{path}'.", path);

        var identity = AssemblyName.GetAssemblyName(path);
        var existing = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly =>
            AssemblyIdentityEquals(assembly.GetName(), identity));
        if (existing != null)
            return;

        _ = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }

    private static int ExecuteWistBoundaryChecks()
    {
        var safeMathPath = ResolveDialectPath("function-calls-safe-math");
        var hostContract = typeof(IBuiltinFunctionDescriptorProvider);
        var providerType = typeof(SafeMathFunctionsCapabilityProvider);
        var implementationContract = providerType.GetInterfaces().SingleOrDefault(candidate =>
            string.Equals(candidate.FullName, hostContract.FullName, StringComparison.Ordinal));
        if (!ReferenceEquals(hostContract, implementationContract) || !hostContract.IsAssignableFrom(providerType))
        {
            throw new InvalidOperationException(
                "SafeMath provider contract has split CLR identity. " +
                $"host={DescribeType(hostContract)}; implementation={DescribeType(implementationContract)}; provider={DescribeType(providerType)}.");
        }

        const string source = """
                              let base = 100.0 * 3.0
                              let discountValue = clamp(base * 0.15, 0.0, 50.0)
                              let result = base - discountValue
                              if result < 0.0 then 0.0 else result
                              """;
        var interpreter = RunWist(safeMathPath, "interpreter", source);
        var cil = RunWist(safeMathPath, "cil", source);
        var interpreterValue = Normalize(interpreter);
        var cilValue = Normalize(cil);
        if (!string.Equals(interpreterValue, "255", StringComparison.Ordinal) ||
            !string.Equals(cilValue, "255", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unexpected SafeMath results: interpreter={interpreterValue}, cil={cilValue}.");
        }

        var interpreterCategory = NormalizeCategory(interpreter);
        var cilCategory = NormalizeCategory(cil);
        if (!string.Equals(interpreterCategory, cilCategory, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Backend CLR value categories differ: interpreter={interpreterCategory}, cil={cilCategory}.");
        }

        var negativeRejected = false;
        try
        {
            _ = RunWist(ResolveDialectPath("minimal-arithmetic"), "interpreter", "clamp(300.0, 0.0, 255.0)");
        }
        catch
        {
            negativeRejected = true;
        }

        if (!negativeRejected)
            throw new InvalidOperationException("SafeMath function surface leaked into minimal-arithmetic.");

        Console.WriteLine("SELECTED_PLAN=canonical-language-plan|SafeMathFunctions|cil,interpreter");
        Console.WriteLine("DIALECT_INSPECT=PASS");
        Console.WriteLine("TYPE_IDENTITY=PASS");
        Console.WriteLine($"INTERPRETER_RESULT={interpreterValue}");
        Console.WriteLine($"CIL_RESULT={cilValue}");
        Console.WriteLine($"CLR_VALUE_CATEGORY={interpreterCategory}");
        Console.WriteLine("BACKEND_PARITY=PASS");
        Console.WriteLine("NEGATIVE_SURFACE=PASS");
        return 0;
    }

    private static object? RunWist(string dialectPath, string backend, string source)
    {
        var options = WistEngineOptions.FromDialectFile(dialectPath);
        options.BackendId = backend;
        using var engine = WistEngine.Create(options);
        return engine.Evaluate<object?>(source);
    }

    private static string ResolveDialectPath(string presetId) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "Dialects",
            "examples",
            "wist",
            presetId,
            "dialect.wistdialect"));

    private static string Normalize(object? value) => value switch
    {
        double doubleValue => doubleValue.ToString("G17", CultureInfo.InvariantCulture),
        float floatValue => floatValue.ToString("G9", CultureInfo.InvariantCulture),
        null => "<null>",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? "<unknown>"
    };

    private static string NormalizeCategory(object? value) =>
        value?.GetType().FullName ?? "null";

    private static int RunHostilePreload(IReadOnlyList<string> args)
    {
        if (args.Count != 3)
            throw new ArgumentException("Hostile mode requires canonical and hostile assembly paths.");

        var canonicalPath = Path.GetFullPath(args[1]);
        var hostilePath = Path.GetFullPath(args[2]);
        var hostile = AssemblyLoadContext.Default.LoadFromAssemblyPath(hostilePath);
        if (hostile.GetName().Version != new Version(2, 0, 0, 0))
            throw new InvalidOperationException("Hostile fixture version is not 2.0.0.0.");

        using var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new StaticLocator(RuntimeFixtureAssemblyName, canonicalPath),
            new DefaultRuntimeSharedAssemblyResolver([]));
        var loaded = strategy.LoadAssembly(RuntimeFixtureAssemblyName);
        var context = AssemblyLoadContext.GetLoadContext(loaded);

        if (ReferenceEquals(loaded, hostile) ||
            loaded.GetName().Version != new Version(1, 0, 0, 0) ||
            !string.Equals(context?.Name, "UniversalToolchain.Runtime.Isolated", StringComparison.Ordinal) ||
            !string.Equals(Path.GetFullPath(loaded.Location), canonicalPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Hostile default-context preload became authority. loaded={loaded.FullName}, context={context?.Name}, path={loaded.Location}.");
        }

        Console.WriteLine("HOSTILE_PRELOAD=PASS");
        return 0;
    }

    private static int RunUnregisteredDefaultFallback(IReadOnlyList<string> args)
    {
        if (args.Count != 2)
            throw new ArgumentException("Unregistered-default-fallback mode requires the runtime fixture path.");

        _ = typeof(IHostOnlyContract).Assembly;
        var runtimePath = Path.GetFullPath(args[1]);
        using var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new StaticLocator(UnregisteredDependencyFixtureAssemblyName, runtimePath),
            new DefaultRuntimeSharedAssemblyResolver([]));

        try
        {
            var loaded = strategy.LoadAssembly(UnregisteredDependencyFixtureAssemblyName);
            var componentType = loaded.GetType(
                "UnregisteredDependencyRuntimeFixture.RuntimeComponent",
                throwOnError: true,
                ignoreCase: false)!;
            _ = componentType.GetInterfaces();
        }
        catch (Exception exception) when (ContainsForbiddenDefaultFallbackDiagnostic(exception))
        {
            Console.WriteLine("UNREGISTERED_DEFAULT_FALLBACK=PASS");
            return 0;
        }

        throw new InvalidOperationException(
            "An unregistered host-only dependency was silently resolved from the default context.");
    }

    private static bool ContainsForbiddenDefaultFallbackDiagnostic(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains(
                    "Fallback to an assembly from the default context is forbidden",
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (current is ReflectionTypeLoadException reflectionTypeLoadException &&
                reflectionTypeLoadException.LoaderExceptions.Any(static loaderException =>
                    loaderException != null &&
                    loaderException.Message.Contains(
                        "Fallback to an assembly from the default context is forbidden",
                        StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private static string DescribeType(Type? type) =>
        type == null
            ? "<null>"
            : $"{type.AssemblyQualifiedName} [{DescribeAssembly(type.Assembly)}]";

    private static string DescribeAssembly(Assembly assembly)
    {
        var context = AssemblyLoadContext.GetLoadContext(assembly);
        return $"{assembly.FullName}; context={context?.Name ?? "<null>"}; location={assembly.Location}";
    }

    private static bool AssemblyIdentityEquals(AssemblyName left, AssemblyName right) =>
        string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
        left.Version == right.Version &&
        string.Equals(NormalizeCulture(left.CultureName), NormalizeCulture(right.CultureName), StringComparison.Ordinal) &&
        left.GetPublicKeyToken().AsSpan().SequenceEqual(right.GetPublicKeyToken());

    private static string NormalizeCulture(string? culture) =>
        string.IsNullOrWhiteSpace(culture) ? string.Empty : culture.ToLowerInvariant();

    private sealed class StaticLocator(string simpleName, string path) : IRuntimeAssemblyLocator
    {
        public bool TryResolveAssemblyPath(string assemblySimpleName, out string assemblyPath)
        {
            if (string.Equals(simpleName, assemblySimpleName, StringComparison.Ordinal))
            {
                assemblyPath = path;
                return true;
            }

            assemblyPath = string.Empty;
            return false;
        }
    }
}
