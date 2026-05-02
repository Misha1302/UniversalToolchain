using System.Collections.ObjectModel;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ExpressionTyping.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class CapabilityCatalog
{
    private readonly ReadOnlyCollection<BuiltinFunctionDescriptor> _builtinFunctionDescriptors;
    private readonly ReadOnlyCollection<BuiltinFunctionRuntimeBinding> _builtinFunctionRuntimeBindings;
    private readonly ReadOnlyCollection<ToolchainDiagnostic> _diagnostics;
    private readonly ReadOnlyCollection<IExpressionTypeRule> _expressionTypeRules;
    private readonly IReadOnlyDictionary<LanguageFeatureId, CapabilityProviderDescriptor> _featureOwnersById;
    private readonly ReadOnlyCollection<LanguageFeatureDescriptor> _languageFeatures;
    private readonly ReadOnlyCollection<CapabilityProviderDescriptor> _providers;

    public CapabilityCatalog(
        IEnumerable<CapabilityProviderDescriptor> providers,
        IEnumerable<LanguageFeatureDescriptor> languageFeatures,
        IEnumerable<BuiltinFunctionDescriptor> builtinFunctionDescriptors,
        IEnumerable<BuiltinFunctionRuntimeBinding> builtinFunctionRuntimeBindings,
        IEnumerable<IExpressionTypeRule> expressionTypeRules,
        IEnumerable<ToolchainDiagnostic> diagnostics,
        IReadOnlyDictionary<LanguageFeatureId, CapabilityProviderDescriptor>? featureOwnersById = null)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(languageFeatures);
        ArgumentNullException.ThrowIfNull(builtinFunctionDescriptors);
        ArgumentNullException.ThrowIfNull(builtinFunctionRuntimeBindings);
        ArgumentNullException.ThrowIfNull(expressionTypeRules);
        ArgumentNullException.ThrowIfNull(diagnostics);

        _providers = new ReadOnlyCollection<CapabilityProviderDescriptor>(providers
            .OrderBy(static x => CapabilityProviderTypeResolver.GetTypeName(x.RuntimeComponentImplementationType), StringComparer.Ordinal)
            .ThenBy(static x => CapabilityProviderTypeResolver.GetTypeName(x.ProviderType), StringComparer.Ordinal)
            .ToList());
        _languageFeatures = new ReadOnlyCollection<LanguageFeatureDescriptor>(languageFeatures
            .OrderBy(static x => x.FeatureId.Value, StringComparer.Ordinal)
            .ThenBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList());
        _builtinFunctionDescriptors = new ReadOnlyCollection<BuiltinFunctionDescriptor>(builtinFunctionDescriptors
            .OrderBy(static x => x.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.Parameters.Count)
            .ThenBy(GetParameterTypeSortKey, StringComparer.Ordinal)
            .ToList());
        _builtinFunctionRuntimeBindings = new ReadOnlyCollection<BuiltinFunctionRuntimeBinding>(builtinFunctionRuntimeBindings
            .OrderBy(static x => x.Signature.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.Signature.ParameterTypes.Count)
            .ThenBy(GetParameterTypeSortKey, StringComparer.Ordinal)
            .ToList());
        _expressionTypeRules = new ReadOnlyCollection<IExpressionTypeRule>(expressionTypeRules
            .OrderBy(static x => CapabilityProviderTypeResolver.GetTypeName(x.GetType()), StringComparer.Ordinal)
            .ToList());
        _diagnostics = new ReadOnlyCollection<ToolchainDiagnostic>(diagnostics
            .OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Message, StringComparer.Ordinal)
            .ToList());
        _featureOwnersById = featureOwnersById ?? new Dictionary<LanguageFeatureId, CapabilityProviderDescriptor>();
    }

    public IReadOnlyList<CapabilityProviderDescriptor> Providers => _providers;

    public IReadOnlyList<LanguageFeatureDescriptor> LanguageFeatures => _languageFeatures;

    public IReadOnlyList<BuiltinFunctionDescriptor> BuiltinFunctionDescriptors => _builtinFunctionDescriptors;

    public IReadOnlyList<BuiltinFunctionRuntimeBinding> BuiltinFunctionRuntimeBindings => _builtinFunctionRuntimeBindings;

    public IReadOnlyList<IExpressionTypeRule> ExpressionTypeRules => _expressionTypeRules;

    public IReadOnlyList<ToolchainDiagnostic> Diagnostics => _diagnostics;

    public bool ContainsProvider(Type providerType)
    {
        ArgumentNullException.ThrowIfNull(providerType);

        return _providers.Any(x => x.ProviderType == providerType);
    }

    public bool TryGetOwningProvider(LanguageFeatureId featureId, out CapabilityProviderDescriptor descriptor) => _featureOwnersById.TryGetValue(featureId, out descriptor!);

    internal static CapabilityCatalog Build(
        IEnumerable<Type> runtimeComponentImplementationTypes,
        CapabilityProviderTypeResolver providerTypeResolver,
        CapabilityProviderFactory providerFactory)
    {
        ArgumentNullException.ThrowIfNull(runtimeComponentImplementationTypes);
        ArgumentNullException.ThrowIfNull(providerTypeResolver);
        ArgumentNullException.ThrowIfNull(providerFactory);

        var discovery = providerTypeResolver.Resolve(runtimeComponentImplementationTypes);
        var providers = new List<CapabilityProviderDescriptor>();
        var features = new List<LanguageFeatureDescriptor>();
        var functions = new List<BuiltinFunctionDescriptor>();
        var runtimeBindings = new List<BuiltinFunctionRuntimeBinding>();
        var expressionTypeRules = new List<IExpressionTypeRule>();
        var diagnostics = new List<ToolchainDiagnostic>(discovery.Diagnostics);
        var featureOwnersById = new Dictionary<LanguageFeatureId, CapabilityProviderDescriptor>();

        foreach (var descriptor in discovery.ProviderDescriptors)
        {
            if (!providerFactory.TryCreate(descriptor, out var provider, out var diagnostic))
            {
                if (diagnostic != null)
                    diagnostics.Add(diagnostic);

                continue;
            }

            providers.Add(descriptor);

            if (provider is ILanguageFeatureDescriptorProvider languageFeatureDescriptorProvider)
                foreach (var feature in languageFeatureDescriptorProvider.GetLanguageFeatures() ?? [])
                {
                    features.Add(feature);
                    featureOwnersById.TryAdd(feature.FeatureId, descriptor);
                }

            if (provider is IBuiltinFunctionDescriptorProvider builtinFunctionDescriptorProvider)
                functions.AddRange(builtinFunctionDescriptorProvider.GetFunctions() ?? []);

            if (provider is IBuiltinFunctionRuntimeBindingProvider builtinFunctionRuntimeBindingProvider)
                runtimeBindings.AddRange(builtinFunctionRuntimeBindingProvider.GetRuntimeBindings() ?? []);

            if (provider is IExpressionTypeRuleProvider expressionTypeRuleProvider)
                expressionTypeRules.AddRange(expressionTypeRuleProvider.GetRules() ?? []);
        }

        return new CapabilityCatalog(
            providers,
            features,
            functions,
            runtimeBindings,
            expressionTypeRules,
            diagnostics,
            featureOwnersById);
    }

    private static string GetParameterTypeSortKey(BuiltinFunctionDescriptor descriptor)
    {
        return string.Join("|", descriptor.Parameters.Select(static x => x.Type.Name));
    }

    private static string GetParameterTypeSortKey(BuiltinFunctionRuntimeBinding binding)
    {
        return string.Join("|", binding.Signature.ParameterTypes.Select(static x => x.Name));
    }
}