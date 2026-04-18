using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend.Composition;

public sealed class DialectDslRegistryBuilder
{
    private readonly List<IDialectDocumentValidationRule> _documentRules = [];
    private readonly List<IDialectDirectiveFeature> _features = [];

    public DialectDslRegistryBuilder RegisterFeature(IDialectDirectiveFeature feature)
    {
        feature = feature.ArgNotNull();

        _features.Add(feature);
        return this;
    }

    public DialectDslRegistryBuilder RegisterDocumentRule(IDialectDocumentValidationRule rule)
    {
        rule = rule.ArgNotNull();

        _documentRules.Add(rule);
        return this;
    }

    public DialectDslRegistry Build() => new(_features, _documentRules);
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
            builder.RegisterFeature(feature);

        foreach (var rule in Snapshot(documentRules, nameof(documentRules)))
            builder.RegisterDocumentRule(rule);

        var orderedProviders = Snapshot(providers, nameof(providers))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        foreach (var provider in orderedProviders)
            provider.Register(builder);

        return builder.Build();
    }

    private static List<T> Snapshot<T>(IEnumerable<T> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        var result = new List<T>();
        foreach (var value in values)
        {
            if (value == null)
                Thrower.Argument(paramName.NotNull(), "Collection must not contain null values.");

            result.Add(value);
        }

        return result;
    }
}

public sealed class DialectDslRegistry
{
    private readonly IReadOnlyDictionary<string, IDialectDirectiveFeature> _featuresById;
    private readonly IReadOnlyDictionary<string, IDialectDirectiveFeature> _featuresByKeyword;

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

        DirectiveFeatures = features;
        DocumentRules = Snapshot(documentRules, nameof(documentRules))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();
        _featuresByKeyword = DirectiveFeatures.ToDictionary(x => x.Keyword, StringComparer.Ordinal);
        _featuresById = DirectiveFeatures.ToDictionary(x => x.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<IDialectDirectiveFeature> DirectiveFeatures { get; }

    public IReadOnlyList<IDialectDocumentValidationRule> DocumentRules { get; }

    public bool TryGetFeature(string keyword, out IDialectDirectiveFeature feature)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            Thrower.Argument(nameof(keyword), "Directive keyword must not be empty.");

        return _featuresByKeyword.TryGetValue(keyword, out feature!);
    }

    public IDialectDirectiveFeature GetFeatureById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            Thrower.Argument(nameof(id), "Directive identifier must not be empty.");

        if (!_featuresById.TryGetValue(id, out var feature))
            Thrower.Argument(nameof(id), $"Unknown dialect directive identifier '{id}'.");

        return feature;
    }

    private static List<T> Snapshot<T>(IEnumerable<T> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        var result = new List<T>();
        foreach (var value in values)
        {
            if (value == null)
                Thrower.Argument(paramName.NotNull(), "Collection must not contain null values.");

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
            Thrower.InvalidOpEx($"Dialect DSL keyword '{duplicateKeyword.Key}' is implemented by multiple features.");

        var duplicateId = features
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateId != null)
            Thrower.InvalidOpEx($"Dialect directive identifier '{duplicateId.Key}' is implemented by multiple features.");

        DialectParserOrderValidation.EnsureNoCollisions(
            features,
            static x => DialectParserOrder.Directive(x.ParserOrder),
            static x => $"{x.Id} ({x.GetType().FullName})",
            "directive features");
    }
}