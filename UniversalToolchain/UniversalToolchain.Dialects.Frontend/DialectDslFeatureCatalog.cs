using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public enum DialectParserStage
{
    LineSplitting = 0,
    Declaration = 1,
    Directives = 2,
    Document = 3
}

public enum DialectDirectiveSlot
{
    ModuleSelection = 0,
    ModuleOrdering = 1,
    BackendSelection = 2,
    IntrinsicPolicy = 3,
    OptimizerPolicy = 4,
    Security = 5,
    Capabilities = 6,
    Extension = 100
}

public readonly record struct DialectDirectiveParserOrder(DialectDirectiveSlot Slot, int Sequence);

public readonly record struct DialectParserOrder(DialectParserStage Stage, int Slot, int Sequence)
{
    internal float Encode()
    {
        return ((int)Stage * 100000f) + (Slot * 100f) + Sequence;
    }

    public static DialectParserOrder Directive(DialectDirectiveParserOrder order)
    {
        return new DialectParserOrder(DialectParserStage.Directives, (int)order.Slot, order.Sequence);
    }
}

public static class DialectParserOrders
{
    public static DialectParserOrder LineSplitter { get; } = new(DialectParserStage.LineSplitting, 0, 0);

    public static DialectParserOrder Declaration { get; } = new(DialectParserStage.Declaration, 0, 0);

    public static DialectParserOrder Document { get; } = new(DialectParserStage.Document, 0, 0);
}

public interface IDialectDirectiveFeature
{
    string Id { get; }

    string Keyword { get; }

    string LexemeTag { get; }

    DialectDirectiveParserOrder ParserOrder { get; }

    bool IsSingleton { get; }

    DialectDirectiveAstNode ParseDirective(AstNode lineNode);

    void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation);

    void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context);

    IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive);
}

public interface IDialectDocumentValidationRule
{
    int Order { get; }

    void Validate(DialectDocumentAstNode document, DialectDirectiveValidationContext context);
}

public interface IDialectDslFeatureProvider
{
    int Order { get; }

    void Register(DialectDslRegistryBuilder builder);
}

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
            .OrderBy(x => x.ParserOrder.Slot)
            .ThenBy(x => x.ParserOrder.Sequence)
            .ThenBy(x => x.Keyword, StringComparer.Ordinal)
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

    public static DialectDslRegistry BuildFromProviders(IEnumerable<IDialectDslFeatureProvider> providers)
    {
        if (providers == null)
        {
            Thrower.ArgumentNull(nameof(providers));
        }

        var orderedProviders = Snapshot(providers, nameof(providers))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        var builder = new DialectDslRegistryBuilder();
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
    }
}

public static class DialectDslBuiltInFeatures
{
    public static DialectDslRegistry CreateRegistry(IEnumerable<IDialectDslFeatureProvider>? additionalProviders = null)
    {
        var providers = new List<IDialectDslFeatureProvider>
        {
            new BuiltInDialectDslFeatureProvider()
        };

        if (additionalProviders != null)
        {
            providers.AddRange(additionalProviders);
        }

        return DialectDslRegistry.BuildFromProviders(providers);
    }
}

public sealed class DialectDirectiveValidationContext
{
    private readonly Dictionary<string, HashSet<string>> _sets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object> _state = new(StringComparer.Ordinal);

    public IReadOnlySet<string> GetValues(string key)
    {
        return GetOrCreateSet(key);
    }

    public void AddValues(string key, IEnumerable<string> values, string duplicateMessage, LexemeValue? token)
    {
        foreach (var value in values)
        {
            AddValue(key, value, duplicateMessage, token);
        }
    }

    public void AddValue(string key, string value, string duplicateMessage, LexemeValue? token)
    {
        if (!GetOrCreateSet(key).Add(value))
        {
            DialectDefinitionSliceParseErrors.Fail(duplicateMessage, token);
        }
    }

    public void EnsureSingleton(string key, string duplicateMessage, LexemeValue? token)
    {
        if (_state.ContainsKey(key))
        {
            DialectDefinitionSliceParseErrors.Fail(duplicateMessage, token);
        }

        _state[key] = SingletonMarker.Instance;
    }

    public TState GetOrAddState<TState>(string key, Func<TState> factory) where TState : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Thrower.Argument(nameof(key), "Validation state key must not be empty.");
        }

        if (factory == null)
        {
            Thrower.ArgumentNull(nameof(factory));
        }

        if (_state.TryGetValue(key, out var existing))
        {
            if (existing is not TState)
            {
                Thrower.InvalidOpEx<TState>($"Validation state '{key}' has incompatible runtime type '{existing.GetType().FullName}'.");
            }

            return (TState)existing;
        }

        var created = factory();
        if (created == null)
        {
            Thrower.InvalidOpEx<TState>($"Validation state factory for '{key}' returned null.");
        }

        _state[key] = created;
        return created;
    }

    private HashSet<string> GetOrCreateSet(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Thrower.Argument(nameof(key), "Validation set key must not be empty.");
        }

        if (_sets.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = new HashSet<string>(StringComparer.Ordinal);
        _sets[key] = created;
        return created;
    }

    private sealed class SingletonMarker
    {
        public static SingletonMarker Instance { get; } = new();
    }
}

internal abstract class DialectDirectiveFeatureBase : IDialectDirectiveFeature
{
    public abstract string Id { get; }

    public abstract string Keyword { get; }

    public string LexemeTag => $"DialectDirectiveKeyword.{Keyword}";

    public abstract DialectDirectiveParserOrder ParserOrder { get; }

    public virtual bool IsSingleton => false;

    public abstract DialectDirectiveAstNode ParseDirective(AstNode lineNode);

    public virtual void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        Thrower.InvalidOpEx($"Dialect feature '{GetType().Name}' does not support line accumulation.");
    }

    public virtual void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
    }

    public abstract IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive);

    protected static IReadOnlyList<string> GetIdentifierListArgument(DialectDirectiveAstNode directive)
    {
        if (directive.Payload is not IdentifierListAstNode)
        {
            Thrower.Argument(nameof(directive), $"Directive '{directive.Feature.Keyword}' must provide an identifier list payload.");
        }

        var identifierList = (IdentifierListAstNode)directive.Payload;
        if (identifierList.Identifiers.Count == 0)
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directive.Feature.Keyword}' must contain at least one identifier.", directive.LexemeValue);
        }

        foreach (var identifier in identifierList.Identifiers)
        {
            ValidateIdentifier(identifier, $"Directive '{directive.Feature.Keyword}' contains an empty identifier.");
        }

        ValidateNoDuplicates(identifierList.Identifiers.Select(x => x.Identifier), $"Directive '{directive.Feature.Keyword}' contains duplicate identifiers.", directive.LexemeValue);
        return identifierList.Identifiers.Select(x => x.Identifier).ToList();
    }

    protected static string GetSingleIdentifierArgument(DialectDirectiveAstNode directive)
    {
        if (directive.Payload is not IdentifierValueAstNode)
        {
            Thrower.Argument(nameof(directive), $"Directive '{directive.Feature.Keyword}' must provide a single identifier payload.");
        }

        var identifier = (IdentifierValueAstNode)directive.Payload;
        ValidateIdentifier(identifier, $"Directive '{directive.Feature.Keyword}' must not be empty.");
        return identifier.Identifier;
    }

    protected static void ValidateIdentifier(IdentifierValueAstNode identifier, string message)
    {
        if (identifier == null)
        {
            Thrower.ArgumentNull(nameof(identifier));
        }

        if (string.IsNullOrWhiteSpace(identifier.Identifier))
        {
            DialectDefinitionSliceParseErrors.Fail(message, identifier.LexemeValue);
        }
    }

    protected static void ValidateNoDuplicates(IEnumerable<string> values, string message, LexemeValue? token)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!set.Add(value))
            {
                DialectDefinitionSliceParseErrors.Fail(message, token);
            }
        }
    }

    protected static DialectDirectiveAstNode CreateDirectiveNode(IDialectDirectiveFeature feature, LexemeValue? lexemeValue, AstNode payload)
    {
        return new DialectDirectiveAstNode(feature, lexemeValue, [payload]);
    }
}

internal abstract class IdentifierListDialectDirectiveFeatureBase : DialectDirectiveFeatureBase
{
    public sealed override DialectDirectiveAstNode ParseDirective(AstNode lineNode)
    {
        var identifiers = DialectNodeCreatorSupport.ParseIdentifierList(lineNode, Keyword);
        return CreateDirectiveNode(this, lineNode.Children[0].LexemeValue, identifiers);
    }

    public sealed override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        AccumulateIdentifiers(accumulation, DialectDirectiveParserSupport.ParseIdentifierList(line, Keyword));
    }

    protected abstract void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values);
}

internal abstract class SingleIdentifierDialectDirectiveFeatureBase : DialectDirectiveFeatureBase
{
    public sealed override DialectDirectiveAstNode ParseDirective(AstNode lineNode)
    {
        var identifier = DialectNodeCreatorSupport.ParseSingleIdentifier(lineNode, Keyword);
        return CreateDirectiveNode(this, lineNode.Children[0].LexemeValue, identifier);
    }

    public sealed override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        AccumulateIdentifier(accumulation, DialectDirectiveParserSupport.ParseSingleIdentifier(line, Keyword));
    }

    protected abstract void AccumulateIdentifier(DialectDirectiveAccumulation accumulation, string value);
}

internal sealed class UseModulesDialectDirectiveFeature : IdentifierListDialectDirectiveFeatureBase
{
    public const string FeatureId = "builtin.modules.use";
    public const string ValidationKey = FeatureId;

    public override string Id => FeatureId;

    public override string Keyword => DialectDslKeywords.Use;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleSelection, 0);

    protected override void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        accumulation.UseModules.AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(ValidationKey, GetIdentifierListArgument(directive), "Duplicate use module is not allowed.", directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new UseModulesAirAnnotation(GetIdentifierListArgument(directive))];
    }
}

internal sealed class ExcludeModulesDialectDirectiveFeature : IdentifierListDialectDirectiveFeatureBase
{
    public const string FeatureId = "builtin.modules.exclude";
    public const string ValidationKey = FeatureId;

    public override string Id => FeatureId;

    public override string Keyword => DialectDslKeywords.Exclude;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleSelection, 1);

    protected override void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        accumulation.ExcludeModules.AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(ValidationKey, GetIdentifierListArgument(directive), "Duplicate exclude module is not allowed.", directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new ExcludeModulesAirAnnotation(GetIdentifierListArgument(directive))];
    }
}

internal abstract class OrderDialectDirectiveFeatureBase : IdentifierListDialectDirectiveFeatureBase
{
    protected abstract DialectOrderDirectiveKind OrderKind { get; }

    protected abstract string ValidationKey { get; }

    protected abstract string DuplicateMessage { get; }

    protected abstract List<string> GetAccumulationTarget(DialectDirectiveAccumulation accumulation);

    protected override void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        GetAccumulationTarget(accumulation).AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(ValidationKey, GetIdentifierListArgument(directive), DuplicateMessage, directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new OrderAirAnnotation(OrderKind, GetIdentifierListArgument(directive))];
    }
}

internal sealed class RequiresModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    public override string Id => "builtin.order.requires";

    public override string Keyword => DialectDslKeywords.Requires;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleOrdering, 0);

    protected override DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.Requires;

    protected override string ValidationKey => Id;

    protected override string DuplicateMessage => "Duplicate requires module is not allowed.";

    protected override List<string> GetAccumulationTarget(DialectDirectiveAccumulation accumulation) => accumulation.RequiresModules;
}

internal sealed class BeforeModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    public override string Id => "builtin.order.before";

    public override string Keyword => DialectDslKeywords.Before;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleOrdering, 1);

    protected override DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.Before;

    protected override string ValidationKey => Id;

    protected override string DuplicateMessage => "Duplicate before module is not allowed.";

    protected override List<string> GetAccumulationTarget(DialectDirectiveAccumulation accumulation) => accumulation.BeforeModules;
}

internal sealed class AfterModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    public override string Id => "builtin.order.after";

    public override string Keyword => DialectDslKeywords.After;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleOrdering, 2);

    protected override DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.After;

    protected override string ValidationKey => Id;

    protected override string DuplicateMessage => "Duplicate after module is not allowed.";

    protected override List<string> GetAccumulationTarget(DialectDirectiveAccumulation accumulation) => accumulation.AfterModules;
}

internal sealed class BackendDialectDirectiveFeature : IdentifierListDialectDirectiveFeatureBase
{
    public override string Id => "builtin.backends.enable";

    public override string Keyword => DialectDslKeywords.Backend;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.BackendSelection, 0);

    protected override void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        accumulation.Backends.AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(Id, GetIdentifierListArgument(directive), "Duplicate backend identifier is not allowed.", directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new BackendAirAnnotation(GetIdentifierListArgument(directive))];
    }
}

internal abstract class IntrinsicPolicyDialectDirectiveFeatureBase : SingleIdentifierDialectDirectiveFeatureBase
{
    private const string ToggleStateKey = "builtin.intrinsics.toggle";

    protected abstract bool Allowed { get; }

    protected abstract string DuplicateMessage { get; }

    protected abstract string ContradictionMessageTemplate { get; }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        var value = GetSingleIdentifierArgument(directive);
        var state = context.GetOrAddState(ToggleStateKey, static () => new ToggleValidationState());
        state.Add(value, Allowed, DuplicateMessage, ContradictionMessageTemplate, directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new IntrinsicAirAnnotation(GetSingleIdentifierArgument(directive), Allowed)];
    }

    private sealed class ToggleValidationState
    {
        private readonly HashSet<string> _allowed = new(StringComparer.Ordinal);
        private readonly HashSet<string> _forbidden = new(StringComparer.Ordinal);

        public void Add(string value, bool allowed, string duplicateMessage, string contradictionMessageTemplate, LexemeValue? token)
        {
            var current = allowed ? _allowed : _forbidden;
            var opposite = allowed ? _forbidden : _allowed;

            if (!current.Add(value))
            {
                DialectDefinitionSliceParseErrors.Fail(duplicateMessage, token);
            }

            if (opposite.Contains(value))
            {
                DialectDefinitionSliceParseErrors.Fail(string.Format(contradictionMessageTemplate, value), token);
            }
        }
    }
}

internal sealed class AllowIntrinsicDialectDirectiveFeature : IntrinsicPolicyDialectDirectiveFeatureBase
{
    public override string Id => "builtin.intrinsics.allow";

    public override string Keyword => DialectDslKeywords.Allow;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.IntrinsicPolicy, 0);

    protected override bool Allowed => true;

    protected override string DuplicateMessage => "Duplicate allow intrinsic directive is not allowed.";

    protected override string ContradictionMessageTemplate => "Intrinsic '{0}' cannot be both allowed and forbidden.";

    protected override void AccumulateIdentifier(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.AllowedIntrinsics.Add(value);
    }
}

internal sealed class ForbidIntrinsicDialectDirectiveFeature : IntrinsicPolicyDialectDirectiveFeatureBase
{
    public override string Id => "builtin.intrinsics.forbid";

    public override string Keyword => DialectDslKeywords.Forbid;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.IntrinsicPolicy, 1);

    protected override bool Allowed => false;

    protected override string DuplicateMessage => "Duplicate forbid intrinsic directive is not allowed.";

    protected override string ContradictionMessageTemplate => "Intrinsic '{0}' cannot be both allowed and forbidden.";

    protected override void AccumulateIdentifier(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.ForbiddenIntrinsics.Add(value);
    }
}

internal abstract class OptimizerPolicyDialectDirectiveFeatureBase : SingleIdentifierDialectDirectiveFeatureBase
{
    private const string ToggleStateKey = "builtin.optimizers.toggle";

    protected abstract bool Enabled { get; }

    protected abstract string DuplicateMessage { get; }

    protected abstract string ContradictionMessageTemplate { get; }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        var value = GetSingleIdentifierArgument(directive);
        var state = context.GetOrAddState(ToggleStateKey, static () => new ToggleValidationState());
        state.Add(value, Enabled, DuplicateMessage, ContradictionMessageTemplate, directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new OptimizerAirAnnotation(GetSingleIdentifierArgument(directive), Enabled)];
    }

    private sealed class ToggleValidationState
    {
        private readonly HashSet<string> _enabled = new(StringComparer.Ordinal);
        private readonly HashSet<string> _disabled = new(StringComparer.Ordinal);

        public void Add(string value, bool enabled, string duplicateMessage, string contradictionMessageTemplate, LexemeValue? token)
        {
            var current = enabled ? _enabled : _disabled;
            var opposite = enabled ? _disabled : _enabled;

            if (!current.Add(value))
            {
                DialectDefinitionSliceParseErrors.Fail(duplicateMessage, token);
            }

            if (opposite.Contains(value))
            {
                DialectDefinitionSliceParseErrors.Fail(string.Format(contradictionMessageTemplate, value), token);
            }
        }
    }
}

internal sealed class EnableOptimizerDialectDirectiveFeature : OptimizerPolicyDialectDirectiveFeatureBase
{
    public override string Id => "builtin.optimizers.enable";

    public override string Keyword => DialectDslKeywords.Enable;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.OptimizerPolicy, 0);

    protected override bool Enabled => true;

    protected override string DuplicateMessage => "Duplicate enable optimizer directive is not allowed.";

    protected override string ContradictionMessageTemplate => "Optimizer '{0}' cannot be both enabled and disabled.";

    protected override void AccumulateIdentifier(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.EnabledOptimizers.Add(value);
    }
}

internal sealed class DisableOptimizerDialectDirectiveFeature : OptimizerPolicyDialectDirectiveFeatureBase
{
    public override string Id => "builtin.optimizers.disable";

    public override string Keyword => DialectDslKeywords.Disable;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.OptimizerPolicy, 1);

    protected override bool Enabled => false;

    protected override string DuplicateMessage => "Duplicate disable optimizer directive is not allowed.";

    protected override string ContradictionMessageTemplate => "Optimizer '{0}' cannot be both enabled and disabled.";

    protected override void AccumulateIdentifier(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.DisabledOptimizers.Add(value);
    }
}

internal sealed class SecurityDialectDirectiveFeature : SingleIdentifierDialectDirectiveFeatureBase
{
    public override string Id => "builtin.security.profile";

    public override string Keyword => DialectDslKeywords.Security;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Security, 0);

    public override bool IsSingleton => true;

    protected override void AccumulateIdentifier(DialectDirectiveAccumulation accumulation, string value)
    {
        if (accumulation.SecurityProfile != null)
        {
            DialectDefinitionSliceParseErrors.Fail("Security directive can only be declared once.", null);
        }

        accumulation.SecurityProfile = DialectAnnotationValueGuard.ParseSecurityProfile(value);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.EnsureSingleton(Id, "Security directive can only be declared once.", directive.LexemeValue);
        GetSingleIdentifierArgument(directive);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new SecurityAirAnnotation(DialectAnnotationValueGuard.ParseSecurityProfile(GetSingleIdentifierArgument(directive)))];
    }
}

internal sealed class CapabilityDialectDirectiveFeature : IdentifierListDialectDirectiveFeatureBase
{
    public override string Id => "builtin.capabilities.enable";

    public override string Keyword => DialectDslKeywords.Capability;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Capabilities, 0);

    protected override void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        accumulation.Capabilities.AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(Id, GetIdentifierListArgument(directive), "Duplicate capability identifier is not allowed.", directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new CapabilityAirAnnotation(GetIdentifierListArgument(directive))];
    }
}

internal sealed class UseExcludeConflictDocumentValidationRule : IDialectDocumentValidationRule
{
    public int Order => 0;

    public void Validate(DialectDocumentAstNode document, DialectDirectiveValidationContext context)
    {
        foreach (var conflict in context.GetValues(UseModulesDialectDirectiveFeature.ValidationKey).Intersect(context.GetValues(ExcludeModulesDialectDirectiveFeature.ValidationKey), StringComparer.Ordinal))
        {
            DialectDefinitionSliceParseErrors.Fail($"Module '{conflict}' cannot appear in both use and exclude directives.", document.Declaration.NameNode.LexemeValue);
        }
    }
}

internal sealed class BuiltInDialectDslFeatureProvider : IDialectDslFeatureProvider
{
    public int Order => 0;

    public void Register(DialectDslRegistryBuilder builder)
    {
        builder
            .RegisterFeature(new UseModulesDialectDirectiveFeature())
            .RegisterFeature(new ExcludeModulesDialectDirectiveFeature())
            .RegisterFeature(new RequiresModulesDialectDirectiveFeature())
            .RegisterFeature(new BeforeModulesDialectDirectiveFeature())
            .RegisterFeature(new AfterModulesDialectDirectiveFeature())
            .RegisterFeature(new BackendDialectDirectiveFeature())
            .RegisterFeature(new AllowIntrinsicDialectDirectiveFeature())
            .RegisterFeature(new ForbidIntrinsicDialectDirectiveFeature())
            .RegisterFeature(new EnableOptimizerDialectDirectiveFeature())
            .RegisterFeature(new DisableOptimizerDialectDirectiveFeature())
            .RegisterFeature(new SecurityDialectDirectiveFeature())
            .RegisterFeature(new CapabilityDialectDirectiveFeature())
            .RegisterDocumentRule(new UseExcludeConflictDocumentValidationRule());
    }
}

internal static class DialectDirectiveParserSupport
{
    public static string ParseSingleIdentifier(IReadOnlyList<LexemeValue> line, string directiveName)
    {
        if (line.Count != 2 || !DialectLexemeTags.IsTag(line[1], DialectLexemeTags.Identifier))
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' expects exactly one identifier argument.", line.ElementAtOrDefault(1) ?? line[0]);
        }

        return line[1].Text;
    }

    public static IReadOnlyList<string> ParseIdentifierList(IReadOnlyList<LexemeValue> line, string directiveName)
    {
        if (line.Count < 2)
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' expects at least one identifier.", line[0]);
        }

        var values = new List<string>();
        var expectIdentifier = true;
        for (var i = 1; i < line.Count; i++)
        {
            var token = line[i];
            if (expectIdentifier)
            {
                if (!DialectLexemeTags.IsTag(token, DialectLexemeTags.Identifier))
                {
                    DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' contains an invalid identifier list item.", token);
                }

                values.Add(token.Text);
                expectIdentifier = false;
                continue;
            }

            if (!DialectLexemeTags.IsTag(token, DialectLexemeTags.CommaToken))
            {
                DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' expects comma-separated identifiers.", token);
            }

            expectIdentifier = true;
        }

        if (expectIdentifier)
        {
            DialectDefinitionSliceParseErrors.Fail($"Directive '{directiveName}' must not end with a trailing comma.", line[^1]);
        }

        return values;
    }
}
