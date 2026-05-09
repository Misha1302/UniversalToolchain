using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ExpressionTyping.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class CapabilityProviderTypeResolver
{
    public CapabilityDiscoveryResult Resolve(IEnumerable<Type> runtimeComponentImplementationTypes)
    {
        if (runtimeComponentImplementationTypes is null)
            throw new ArgumentNullException(nameof(runtimeComponentImplementationTypes));

        var descriptors = new List<CapabilityProviderDescriptor>();
        var diagnostics = new List<ToolchainDiagnostic>();

        foreach (var componentType in runtimeComponentImplementationTypes
                     .OrderBy(GetTypeName, StringComparer.Ordinal))
        {
            foreach (var attribute in componentType
                         .GetCustomAttributes(typeof(DialectCapabilityProviderAttribute), false)
                         .Cast<DialectCapabilityProviderAttribute>()
                         .OrderBy(static x => GetTypeName(x.ProviderType), StringComparer.Ordinal))
            {
                if (!ImplementsKnownProviderInterface(attribute.ProviderType))
                {
                    diagnostics.Add(CreateInvalidProviderDiagnostic(
                        componentType,
                        attribute.ProviderType,
                        "Capability provider type must implement at least one supported capability provider interface."));
                    continue;
                }

                descriptors.Add(new CapabilityProviderDescriptor(componentType, attribute.ProviderType));
            }
        }

        return new CapabilityDiscoveryResult(
            descriptors
                .OrderBy(static x => GetTypeName(x.RuntimeComponentImplementationType), StringComparer.Ordinal)
                .ThenBy(static x => GetTypeName(x.ProviderType), StringComparer.Ordinal)
                .ToList(),
            diagnostics
                .OrderBy(static x => x.Code, StringComparer.Ordinal)
                .ThenBy(static x => x.Message, StringComparer.Ordinal)
                .ToList());
    }

    internal static bool ImplementsKnownProviderInterface(Type providerType)
    {
        if (providerType is null)
            throw new ArgumentNullException(nameof(providerType));

        return typeof(ILanguageFeatureDescriptorProvider).IsAssignableFrom(providerType) ||
               typeof(IBuiltinFunctionDescriptorProvider).IsAssignableFrom(providerType) ||
               typeof(IBuiltinFunctionRuntimeBindingProvider).IsAssignableFrom(providerType) ||
               typeof(IExpressionTypeRuleProvider).IsAssignableFrom(providerType);
    }

    internal static ToolchainDiagnostic CreateInvalidProviderDiagnostic(
        Type runtimeComponentImplementationType,
        Type providerType,
        string message) =>
        new(
            ToolchainDiagnosticCodes.CapabilityProviderInvalid,
            ToolchainDiagnosticSeverity.Error,
            $"{message} Component='{GetTypeName(runtimeComponentImplementationType)}', provider='{GetTypeName(providerType)}'.",
            null,
            []);

    internal static string GetTypeName(Type type) => type.FullName ?? type.Name;
}