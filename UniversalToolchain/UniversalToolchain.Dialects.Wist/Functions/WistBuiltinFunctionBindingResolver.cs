using System.Reflection;
using CommonExceptions;
using ExceptionsManager;
using SafeMathFunctionsModule;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Wist.Features;
using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Functions.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Functions;

public sealed class WistBuiltinFunctionBindingResolver
{
    private static readonly IReadOnlyDictionary<string, MethodInfo> _methodMap =
        new Dictionary<string, MethodInfo>(StringComparer.Ordinal)
        {
            ["abs"] = typeof(SafeMathFunctions).GetMethod(nameof(SafeMathFunctions.Abs), BindingFlags.Public | BindingFlags.Static).NotNull(),
            ["clamp"] = typeof(SafeMathFunctions).GetMethod(nameof(SafeMathFunctions.Clamp), BindingFlags.Public | BindingFlags.Static).NotNull(),
            ["max"] = typeof(SafeMathFunctions).GetMethod(nameof(SafeMathFunctions.Max), BindingFlags.Public | BindingFlags.Static).NotNull(),
            ["min"] = typeof(SafeMathFunctions).GetMethod(nameof(SafeMathFunctions.Min), BindingFlags.Public | BindingFlags.Static).NotNull()
        };

    private readonly IReadOnlySet<LanguageFeatureId> _availableFeatureIds;
    private readonly IBuiltinFunctionCatalog _builtinFunctionCatalog;
    private readonly IReadOnlySet<string> _selectedAliases;
    private readonly IReadOnlySet<string> _supportedBackendAliases;
    private readonly ILanguageFeatureCatalog _languageFeatureCatalog;

    public WistBuiltinFunctionBindingResolver(
        WistDialectExecutionConfiguration configuration,
        ILanguageFeatureCatalog languageFeatureCatalog,
        IBuiltinFunctionCatalog builtinFunctionCatalog)
    {
        configuration = configuration.ArgNotNull();
        languageFeatureCatalog = languageFeatureCatalog.ArgNotNull();
        builtinFunctionCatalog = builtinFunctionCatalog.ArgNotNull();

        _languageFeatureCatalog = languageFeatureCatalog;
        _builtinFunctionCatalog = builtinFunctionCatalog;
        _selectedAliases = BuildSelectedAliases(configuration);
        _supportedBackendAliases = configuration.EnabledBackends
            .Select(static x => x.CanonicalId)
            .ToHashSet(StringComparer.Ordinal);
        _availableFeatureIds = BuildAvailableFeatureIds();
    }

    public MethodInfo Resolve(string functionName, IReadOnlyList<Type> argumentTypes)
    {
        functionName = functionName.ArgNotNull();
        argumentTypes = argumentTypes.ArgNotNull();

        var functionArgumentTypes = MapArgumentTypes(argumentTypes);
        var namedCandidates = _builtinFunctionCatalog.GetFunctions()
            .Where(x => string.Equals(x.Name, functionName, StringComparison.Ordinal))
            .ToArray();

        if (namedCandidates.Length == 0)
        {
            Fail(RuleDiagnosticCodes.UnknownFunction, $"Unknown function '{functionName}'.");
        }

        var featureCandidates = namedCandidates
            .Where(x => _availableFeatureIds.Contains(x.FeatureId))
            .ToArray();

        if (featureCandidates.Length == 0)
        {
            Fail(
                RuleDiagnosticCodes.FunctionUnavailable,
                $"Function '{functionName}' is unavailable because its feature is not enabled.");
        }

        var backendCandidates = featureCandidates
            .Where(x => _supportedBackendAliases.All(alias => x.SupportedBackendAliases.Contains(alias, StringComparer.Ordinal)))
            .ToArray();

        if (backendCandidates.Length == 0)
        {
            Fail(
                RuleDiagnosticCodes.FunctionUnsupportedBackend,
                $"Function '{functionName}' is not supported by the selected runtime backends.");
        }

        var arityCandidates = backendCandidates
            .Where(x => x.Parameters.Count == functionArgumentTypes.Count)
            .ToArray();

        if (arityCandidates.Length == 0)
        {
            Fail(
                RuleDiagnosticCodes.WrongFunctionArgumentCount,
                $"Function '{functionName}' expects a different argument count.");
        }

        var descriptor = arityCandidates
            .FirstOrDefault(x => HasExactArgumentTypes(x, functionArgumentTypes));

        if (descriptor == null)
        {
            Fail(
                RuleDiagnosticCodes.WrongFunctionArgumentType,
                $"Function '{functionName}' does not support the provided argument types.");
        }

        if (!_methodMap.TryGetValue(functionName, out var method))
        {
            WistThrower.InternalCompiler($"Builtin function '{functionName}' does not have a runtime binding.");
        }

        return method;
    }

    private IReadOnlySet<LanguageFeatureId> BuildAvailableFeatureIds()
    {
        var evaluations = new Dictionary<LanguageFeatureId, bool>();
        var featureIds = new HashSet<LanguageFeatureId>();

        foreach (var descriptor in _languageFeatureCatalog.GetFeatures()
                     .OrderBy(static x => x.FeatureId.Value, StringComparer.Ordinal))
        {
            if (IsFeatureAvailable(descriptor, evaluations, []))
            {
                featureIds.Add(descriptor.FeatureId);
            }
        }

        return featureIds;
    }

    private bool IsFeatureAvailable(
        LanguageFeatureDescriptor descriptor,
        IDictionary<LanguageFeatureId, bool> evaluations,
        IReadOnlyCollection<LanguageFeatureId> path)
    {
        if (evaluations.TryGetValue(descriptor.FeatureId, out var cached))
        {
            return cached;
        }

        if (path.Contains(descriptor.FeatureId))
        {
            evaluations[descriptor.FeatureId] = false;
            return false;
        }

        foreach (var requiredAlias in descriptor.RequiredRuntimeComponentAliases.OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (!_selectedAliases.Contains(requiredAlias))
            {
                evaluations[descriptor.FeatureId] = false;
                return false;
            }
        }

        var nextPath = path.Append(descriptor.FeatureId).ToArray();
        foreach (var requiredFeatureId in descriptor.RequiredFeatures.OrderBy(static x => x.Value, StringComparer.Ordinal))
        {
            if (!_languageFeatureCatalog.TryGetFeature(requiredFeatureId, out var requiredFeatureDescriptor) ||
                requiredFeatureDescriptor == null ||
                !IsFeatureAvailable(requiredFeatureDescriptor, evaluations, nextPath))
            {
                evaluations[descriptor.FeatureId] = false;
                return false;
            }
        }

        evaluations[descriptor.FeatureId] = true;
        return true;
    }

    private static IReadOnlySet<string> BuildSelectedAliases(WistDialectExecutionConfiguration configuration)
    {
        var aliases = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var moduleType in configuration.FrontendModules.Concat(configuration.IrModules).Concat(configuration.Optimizers))
        {
            var export = moduleType.GetCustomAttributes(typeof(DialectRuntimeExportAttribute), false)
                .Cast<DialectRuntimeExportAttribute>()
                .FirstOrDefault();
            if (export != null)
            {
                aliases.Add(export.CanonicalAlias);
            }

            foreach (var aliasAttribute in moduleType.GetCustomAttributes(typeof(DialectAliasAttributeBase), false)
                         .Cast<DialectAliasAttributeBase>())
            {
                foreach (var alias in aliasAttribute.Aliases)
                {
                    aliases.Add(alias);
                }
            }
        }

        foreach (var alias in configuration.EnabledBackends
                     .SelectMany(static x => x.AllNames)
                     .OrderBy(static x => x, StringComparer.Ordinal))
        {
            aliases.Add(alias);
        }

        return aliases;
    }

    private static IReadOnlyList<FunctionTypeDescriptor> MapArgumentTypes(IReadOnlyList<Type> argumentTypes)
    {
        var result = new List<FunctionTypeDescriptor>(argumentTypes.Count);

        foreach (var argumentType in argumentTypes)
        {
            if (IsNumericType(argumentType))
            {
                result.Add(WistFunctionTypeDescriptors.Number);
                continue;
            }

            if (argumentType == typeof(bool))
            {
                result.Add(WistFunctionTypeDescriptors.Bool);
                continue;
            }

            Fail(
                RuleDiagnosticCodes.WrongFunctionArgumentType,
                $"Function arguments of runtime type '{argumentType.FullName ?? argumentType.Name}' are not supported.");
        }

        return result;
    }

    private static bool HasExactArgumentTypes(
        BuiltinFunctionDescriptor descriptor,
        IReadOnlyList<FunctionTypeDescriptor> argumentTypes)
    {
        for (var index = 0; index < descriptor.Parameters.Count; index++)
        {
            if (!Equals(descriptor.Parameters[index].Type, argumentTypes[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNumericType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    private static void Fail(string code, string message)
    {
        WistThrower.Import($"{code}: {message}");
    }
}
