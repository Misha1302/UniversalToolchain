using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Features.Core;
using UniversalToolchain.Functions.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Functions;

public sealed class WistBuiltinFunctionCatalog : IBuiltinFunctionCatalog
{
    private readonly IReadOnlyList<BuiltinFunctionDescriptor> _descriptors;

    public WistBuiltinFunctionCatalog()
        : this(BuildDefaultDescriptors())
    {
    }

    public WistBuiltinFunctionCatalog(IReadOnlyList<BuiltinFunctionDescriptor> descriptors)
    {
        _descriptors = descriptors
            .OrderBy(static x => x.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.Parameters.Count)
            .ThenBy(static x => string.Join(",", x.Parameters.Select(static y => y.Type.Name)), StringComparer.Ordinal)
            .ThenBy(static x => x.ReturnType.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<BuiltinFunctionDescriptor> GetFunctions()
    {
        return _descriptors;
    }

    public BuiltinFunctionResolution Resolve(
        string name,
        IReadOnlyList<FunctionTypeDescriptor> argumentTypes,
        DialectFeatureExplanation featureExplanation,
        string backendAlias)
    {
        var namedCandidates = _descriptors
            .Where(x => string.Equals(x.Name, name, StringComparison.Ordinal))
            .ToArray();

        if (namedCandidates.Length == 0)
        {
            return Failure(
                RuleDiagnosticCodes.UnknownFunction,
                $"Unknown function '{name}'.");
        }

        var availableFeatureIds = featureExplanation.AvailableFeatures
            .Select(static x => x.Descriptor.FeatureId)
            .ToHashSet();

        var featureCandidates = namedCandidates
            .Where(x => availableFeatureIds.Contains(x.FeatureId))
            .ToArray();

        if (featureCandidates.Length == 0)
        {
            return Failure(
                RuleDiagnosticCodes.FunctionUnavailable,
                $"Function '{name}' is unavailable because its feature is not enabled.");
        }

        var backendCandidates = featureCandidates
            .Where(x => x.SupportedBackendAliases.Contains(backendAlias, StringComparer.Ordinal))
            .ToArray();

        if (backendCandidates.Length == 0)
        {
            return Failure(
                RuleDiagnosticCodes.FunctionUnsupportedBackend,
                $"Function '{name}' is not supported by backend '{backendAlias}'.");
        }

        var arityCandidates = backendCandidates
            .Where(x => x.Parameters.Count == argumentTypes.Count)
            .ToArray();

        if (arityCandidates.Length == 0)
        {
            return Failure(
                RuleDiagnosticCodes.WrongFunctionArgumentCount,
                $"Function '{name}' expects a different argument count.");
        }

        var match = arityCandidates
            .FirstOrDefault(x => HasExactArgumentTypes(x, argumentTypes));

        if (match == null)
        {
            return Failure(
                RuleDiagnosticCodes.WrongFunctionArgumentType,
                $"Function '{name}' does not support the provided argument types.");
        }

        return new BuiltinFunctionResolution(
            true,
            match,
            match.ReturnType,
            []);
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

    private static BuiltinFunctionResolution Failure(string code, string message)
    {
        return new BuiltinFunctionResolution(
            false,
            null,
            null,
            [
                new RuleDiagnostic(
                    code,
                    RuleDiagnosticSeverity.Error,
                    message,
                    null,
                    [])
            ]);
    }

    private static IReadOnlyList<BuiltinFunctionDescriptor> BuildDefaultDescriptors()
    {
        return
        [
            new BuiltinFunctionDescriptor(
                "abs",
                WistLanguageFeatureIds.SafeMathFunctions,
                [new FunctionParameterDescriptor("value", WistFunctionTypeDescriptors.Number)],
                WistFunctionTypeDescriptors.Number,
                FunctionPurity.Pure,
                ["cil", "interpreter"]),
            new BuiltinFunctionDescriptor(
                "clamp",
                WistLanguageFeatureIds.SafeMathFunctions,
                [
                    new FunctionParameterDescriptor("value", WistFunctionTypeDescriptors.Number),
                    new FunctionParameterDescriptor("min", WistFunctionTypeDescriptors.Number),
                    new FunctionParameterDescriptor("max", WistFunctionTypeDescriptors.Number)
                ],
                WistFunctionTypeDescriptors.Number,
                FunctionPurity.Pure,
                ["cil", "interpreter"]),
            new BuiltinFunctionDescriptor(
                "max",
                WistLanguageFeatureIds.SafeMathFunctions,
                [
                    new FunctionParameterDescriptor("left", WistFunctionTypeDescriptors.Number),
                    new FunctionParameterDescriptor("right", WistFunctionTypeDescriptors.Number)
                ],
                WistFunctionTypeDescriptors.Number,
                FunctionPurity.Pure,
                ["cil", "interpreter"]),
            new BuiltinFunctionDescriptor(
                "min",
                WistLanguageFeatureIds.SafeMathFunctions,
                [
                    new FunctionParameterDescriptor("left", WistFunctionTypeDescriptors.Number),
                    new FunctionParameterDescriptor("right", WistFunctionTypeDescriptors.Number)
                ],
                WistFunctionTypeDescriptors.Number,
                FunctionPurity.Pure,
                ["cil", "interpreter"])
        ];
    }
}
