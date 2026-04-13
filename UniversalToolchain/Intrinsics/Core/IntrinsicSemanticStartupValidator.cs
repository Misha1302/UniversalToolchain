using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public sealed class IntrinsicSemanticStartupValidator
{
    private readonly IntrinsicSemanticCoverageValidator _coverageValidator;
    private readonly IntrinsicDescriptorProviderMetadataValidator _metadataValidator;

    public IntrinsicSemanticStartupValidator()
        : this(new IntrinsicDescriptorProviderMetadataValidator(), new IntrinsicSemanticCoverageValidator())
    {
    }

    public IntrinsicSemanticStartupValidator(
        IntrinsicDescriptorProviderMetadataValidator metadataValidator,
        IntrinsicSemanticCoverageValidator coverageValidator)
    {
        metadataValidator = metadataValidator.ArgNotNull();

        coverageValidator = coverageValidator.ArgNotNull();

        _metadataValidator = metadataValidator;
        _coverageValidator = coverageValidator;
    }

    public IntrinsicSemanticValidationResult BuildResult(
        IEnumerable<IIntrinsicDescriptorProvider> providers,
        IEnumerable<(Type ModuleType, Type ProviderType)>? coverageRequirements = null)
    {
        providers = providers.ArgNotNull();

        var providerList = providers.ToList();
        var resolvedCoverageRequirements = SnapshotCoverageRequirements(coverageRequirements);
        var errors = new List<string>();

        errors.AddRange(_metadataValidator.Validate(providerList));

        IIntrinsicCatalog? catalog = null;
        if (errors.Count == 0)
        {
            try
            {
                catalog = new IntrinsicCatalogBuilder().Build(providerList);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex.Message);
            }
        }

        if (catalog != null)
            errors.AddRange(_coverageValidator.Validate(catalog, providerList, resolvedCoverageRequirements));

        return new IntrinsicSemanticValidationResult(errors);
    }

    public IntrinsicSemanticValidationResult Validate(
        IEnumerable<IIntrinsicDescriptorProvider> providers,
        IEnumerable<(Type ModuleType, Type ProviderType)>? coverageRequirements = null)
    {
        var result = BuildResult(providers, coverageRequirements);
        if (!result.IsSuccess)
            Thrower.InvalidOpEx("Intrinsic semantic startup validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, result.Errors));

        return result;
    }

    private static IReadOnlyList<(Type ModuleType, Type ProviderType)> SnapshotCoverageRequirements(
        IEnumerable<(Type ModuleType, Type ProviderType)>? coverageRequirements)
    {
        if (coverageRequirements == null)
            return [];

        return coverageRequirements
            .Select(x => (x.ModuleType.NotNull(nameof(coverageRequirements)), x.ProviderType.NotNull(nameof(coverageRequirements))))
            .OrderBy(x => x.Item1.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.Item2.FullName, StringComparer.Ordinal)
            .ToList();
    }
}
