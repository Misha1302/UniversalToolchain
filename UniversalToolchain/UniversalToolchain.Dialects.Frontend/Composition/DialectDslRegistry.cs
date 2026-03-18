using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslRegistryBuilder
{
    private readonly List<IDialectDirectiveFeature> _features = [];
    private readonly List<IDialectDocumentValidationRule> _documentRules = [];

    public DialectDslRegistryBuilder RegisterFeature(IDialectDirectiveFeature feature)
    {
        if (feature == null)
        {
            Thrower.ArgumentNull(nameof(feature));
        }

        _features.Add(feature);
        return this;
    }

    public DialectDslRegistryBuilder RegisterDocumentRule(IDialectDocumentValidationRule rule)
    {
        if (rule == null)
        {
            Thrower.ArgumentNull(nameof(rule));
        }

        _documentRules.Add(rule);
        return this;
    }

    public DialectDslRegistry Build()
    {
        return new DialectDslRegistry(_features, _documentRules);
    }
}

public interface IDialectDslRegistryFactory
{
    DialectDslRegistry CreateRegistry();
}

public sealed class DialectDslRegistryFactory(
    IEnumerable<IDialectDirectiveFeature> directiveFeatures,
    IEnumerable<IDialectDocumentValidationRule> documentRules,
    IEnumerable<IDialectDslFeatureProvider> providers) : IDialectDslRegistryFactory
{
    public DialectDslRegistry CreateRegistry()
    {
        var builder = new DialectDslRegistryBuilder();

        foreach (var feature in Snapshot(directiveFeatures, nameof(directiveFeatures)))
        {
            builder.RegisterFeature(feature);
        }

        foreach (var rule in Snapshot(documentRules, nameof(documentRules)))
        {
            builder.RegisterDocumentRule(rule);
        }

        var orderedProviders = Snapshot(providers, nameof(providers))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        foreach (var provider in orderedProviders)
        {
            provider.Register(builder);
        }

        return builder.Build();
    }

    private static List<T> Snapshot<T>(IEnumerable<T> values, string paramName)
    {
        if (values == null)
        {
            Thrower.ArgumentNull(paramName);
        }

        var result = new List<T>();
        foreach (var value in values)
        {
            if (value == null)
            {
                Thrower.Argument(paramName, "Collection must not contain null values.");
            }

            result.Add(value);
        }

        return result;
    }
}

public sealed class DialectDslRegistry
{
    private readonly IReadOnlyList<IDialectDirectiveFeature> _directiveFeatures;
    private readonly IReadOnlyList<IDialectDocumentValidationRule> _documentRules;
    private readonly IReadOnlyDictionary<string, IDialectDirectiveFeature> _featuresByKeyword;
    private readonly IReadOnlyDictionary<string, IDialectDirectiveFeature> _featuresById;

    public DialectDslRegistry(
        IEnumerable<IDialectDirectiveFeature> directiveFeatures,
        IEnumerable<IDialectDocumentValidationRule> documentRules)
    {
        var features = Snapshot(directiveFeatures, nameof(directiveFeatures))
            .OrderBy(x => x.ParserOrder)
            .ThenBy(x => x.Keyword, StringComparer.Ordinal)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        ValidateFeatures(features);

        _directiveFeatures = features;
        _documentRules = Snapshot(documentRules, nameof(documentRules))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();
        _featuresByKeyword = _directiveFeatures.ToDictionary(x => x.Keyword, StringComparer.Ordinal);
        _featuresById = _directiveFeatures.ToDictionary(x => x.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<IDialectDirectiveFeature> DirectiveFeatures => _directiveFeatures;

    public IReadOnlyList<IDialectDocumentValidationRule> DocumentRules => _documentRules;

    public bool TryGetFeature(string keyword, out IDialectDirectiveFeature feature)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            Thrower.Argument(nameof(keyword), "Directive keyword must not be empty.");
        }

        return _featuresByKeyword.TryGetValue(keyword, out feature!);
    }

    public IDialectDirectiveFeature GetFeatureById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Thrower.Argument(nameof(id), "Directive identifier must not be empty.");
        }

        if (!_featuresById.TryGetValue(id, out var feature))
        {
            Thrower.Argument(nameof(id), $"Unknown dialect directive identifier '{id}'.");
        }

        return feature;
    }

    private static List<T> Snapshot<T>(IEnumerable<T> values, string paramName)
    {
        if (values == null)
        {
            Thrower.ArgumentNull(paramName);
        }

        var result = new List<T>();
        foreach (var value in values)
        {
            if (value == null)
            {
                Thrower.Argument(paramName, "Collection must not contain null values.");
            }

            result.Add(value);
        }

        return result;
    }

    private static void ValidateFeatures(IReadOnlyList<IDialectDirectiveFeature> features)
    {
        var duplicateKeyword = features
            .GroupBy(x => x.Keyword, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateKeyword != null)
        {
            Thrower.InvalidOpEx($"Dialect DSL keyword '{duplicateKeyword.Key}' is implemented by multiple features.");
        }

        var duplicateId = features
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateId != null)
        {
            Thrower.InvalidOpEx($"Dialect directive identifier '{duplicateId.Key}' is implemented by multiple features.");
        }

        DialectParserOrderValidation.EnsureNoCollisions(
            features,
            static x => DialectParserOrder.Directive(x.ParserOrder),
            static x => $"{x.Id} ({x.GetType().FullName})",
            "directive features");
    }
}
