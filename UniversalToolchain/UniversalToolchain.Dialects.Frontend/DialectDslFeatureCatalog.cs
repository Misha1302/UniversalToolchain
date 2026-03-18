using System.Reflection;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public enum DialectDirectiveKind
{
    UseModules,
    ExcludeModules,
    RequiresModules,
    BeforeModules,
    AfterModules,
    Backend,
    AllowIntrinsic,
    ForbidIntrinsic,
    EnableIntrinsic,
    DisableIntrinsic,
    Security,
    Capability
}

public enum DialectDirectiveArgumentShape
{
    Identifier,
    IdentifierList
}

public interface IDialectDirectiveFeature
{
    DialectDirectiveKind Kind { get; }

    string Keyword { get; }

    string LexemeTag { get; }

    DialectDirectiveArgumentShape ArgumentShape { get; }

    float ParserPriority { get; }

    bool IsSingleton { get; }

    IAstNodeCreator CreateNodeCreator();

    void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation);

    void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state);

    IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive);
}

public interface IDialectDocumentValidationRule
{
    int Order { get; }

    void Validate(DialectDocumentAstNode document, DialectDirectiveValidationState state);
}

public static class DialectDslFeatureCatalog
{
    private static readonly Lazy<IReadOnlyList<IDialectDirectiveFeature>> _features = new(DiscoverFeatures);
    private static readonly Lazy<IReadOnlyDictionary<DialectDirectiveKind, IDialectDirectiveFeature>> _featuresByKind = new(() =>
        _features.Value.ToDictionary(x => x.Kind));
    private static readonly Lazy<IReadOnlyDictionary<string, IDialectDirectiveFeature>> _featuresByKeyword = new(() =>
        _features.Value.ToDictionary(x => x.Keyword, StringComparer.Ordinal));
    private static readonly Lazy<IReadOnlyList<IDialectDocumentValidationRule>> _documentRules = new(DiscoverDocumentRules);

    public static IReadOnlyList<IDialectDirectiveFeature> Features => _features.Value;

    public static IReadOnlyList<IDialectDocumentValidationRule> DocumentRules => _documentRules.Value;

    public static IDialectDirectiveFeature GetFeature(DialectDirectiveKind kind)
    {
        if (!_featuresByKind.Value.TryGetValue(kind, out var feature))
        {
            Thrower.Argument(nameof(kind), $"Unknown dialect directive kind '{kind}'.");
        }

        return feature;
    }

    public static bool TryGetFeature(string keyword, out IDialectDirectiveFeature feature)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            Thrower.Argument(nameof(keyword), "Directive keyword must not be empty.");
        }

        return _featuresByKeyword.Value.TryGetValue(keyword, out feature!);
    }

    private static IReadOnlyList<IDialectDirectiveFeature> DiscoverFeatures()
    {
        var features = typeof(DialectDslFeatureCatalog).Assembly
            .GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(IDialectDirectiveFeature).IsAssignableFrom(x))
            .Select(Create<IDialectDirectiveFeature>)
            .OrderBy(x => x.ParserPriority)
            .ThenBy(x => x.Keyword, StringComparer.Ordinal)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        ValidateFeatureSet(features);
        return features;
    }

    private static IReadOnlyList<IDialectDocumentValidationRule> DiscoverDocumentRules()
    {
        return typeof(DialectDslFeatureCatalog).Assembly
            .GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(IDialectDocumentValidationRule).IsAssignableFrom(x))
            .Select(Create<IDialectDocumentValidationRule>)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static T Create<T>(Type type)
    {
        var instance = Activator.CreateInstance(type);
        if (instance is not T)
        {
            Thrower.InvalidOpEx<T>($"Could not create dialect feature instance '{type.FullName}'.");
        }

        return (T)instance;
    }

    private static void ValidateFeatureSet(IReadOnlyList<IDialectDirectiveFeature> features)
    {
        var duplicateKeyword = features
            .GroupBy(x => x.Keyword, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateKeyword != null)
        {
            Thrower.InvalidOpEx($"Dialect DSL keyword '{duplicateKeyword.Key}' is implemented by multiple features.");
        }

        var duplicateKind = features
            .GroupBy(x => x.Kind)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateKind != null)
        {
            Thrower.InvalidOpEx($"Dialect directive kind '{duplicateKind.Key}' is implemented by multiple features.");
        }
    }
}

public sealed class DialectDirectiveValidationState
{
    private readonly HashSet<string> _useModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _excludeModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _requiresModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _beforeModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _afterModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _backends = new(StringComparer.Ordinal);
    private readonly HashSet<string> _capabilities = new(StringComparer.Ordinal);
    private readonly HashSet<string> _allowedIntrinsics = new(StringComparer.Ordinal);
    private readonly HashSet<string> _forbiddenIntrinsics = new(StringComparer.Ordinal);
    private readonly HashSet<string> _enabledOptimizers = new(StringComparer.Ordinal);
    private readonly HashSet<string> _disabledOptimizers = new(StringComparer.Ordinal);

    public IReadOnlySet<string> UseModules => _useModules;

    public IReadOnlySet<string> ExcludeModules => _excludeModules;

    public bool HasSecurityDirective { get; private set; }

    public void AddUseModules(IEnumerable<string> modules, LexemeValue? token) => AddMany(_useModules, modules, "Duplicate use module is not allowed.", token);

    public void AddExcludeModules(IEnumerable<string> modules, LexemeValue? token) => AddMany(_excludeModules, modules, "Duplicate exclude module is not allowed.", token);

    public void AddRequiresModules(IEnumerable<string> modules, LexemeValue? token) => AddMany(_requiresModules, modules, "Duplicate requires module is not allowed.", token);

    public void AddBeforeModules(IEnumerable<string> modules, LexemeValue? token) => AddMany(_beforeModules, modules, "Duplicate before module is not allowed.", token);

    public void AddAfterModules(IEnumerable<string> modules, LexemeValue? token) => AddMany(_afterModules, modules, "Duplicate after module is not allowed.", token);

    public void AddBackends(IEnumerable<string> backends, LexemeValue? token) => AddMany(_backends, backends, "Duplicate backend identifier is not allowed.", token);

    public void AddCapabilities(IEnumerable<string> capabilities, LexemeValue? token) => AddMany(_capabilities, capabilities, "Duplicate capability identifier is not allowed.", token);

    public void AddAllowedIntrinsic(string name, LexemeValue? token)
    {
        AddSingle(_allowedIntrinsics, name, "Duplicate allow intrinsic directive is not allowed.", token);
        if (_forbiddenIntrinsics.Contains(name))
        {
            DialectDefinitionSliceParseErrors.Fail($"Intrinsic '{name}' cannot be both allowed and forbidden.", token);
        }
    }

    public void AddForbiddenIntrinsic(string name, LexemeValue? token)
    {
        AddSingle(_forbiddenIntrinsics, name, "Duplicate forbid intrinsic directive is not allowed.", token);
        if (_allowedIntrinsics.Contains(name))
        {
            DialectDefinitionSliceParseErrors.Fail($"Intrinsic '{name}' cannot be both allowed and forbidden.", token);
        }
    }

    public void AddEnabledOptimizer(string name, LexemeValue? token)
    {
        AddSingle(_enabledOptimizers, name, "Duplicate enable directive is not allowed.", token);
        if (_disabledOptimizers.Contains(name))
        {
            DialectDefinitionSliceParseErrors.Fail($"Optimizer '{name}' cannot be both enabled and disabled.", token);
        }
    }

    public void AddDisabledOptimizer(string name, LexemeValue? token)
    {
        AddSingle(_disabledOptimizers, name, "Duplicate disable directive is not allowed.", token);
        if (_enabledOptimizers.Contains(name))
        {
            DialectDefinitionSliceParseErrors.Fail($"Optimizer '{name}' cannot be both enabled and disabled.", token);
        }
    }

    public void MarkSecurity(LexemeValue? token)
    {
        if (HasSecurityDirective)
        {
            DialectDefinitionSliceParseErrors.Fail("Security directive can only be declared once.", token);
        }

        HasSecurityDirective = true;
    }

    private static void AddMany(HashSet<string> set, IEnumerable<string> values, string duplicateMessage, LexemeValue? token)
    {
        foreach (var value in values)
        {
            AddSingle(set, value, duplicateMessage, token);
        }
    }

    private static void AddSingle(HashSet<string> set, string value, string duplicateMessage, LexemeValue? token)
    {
        if (!set.Add(value))
        {
            DialectDefinitionSliceParseErrors.Fail(duplicateMessage, token);
        }
    }
}

internal abstract class DialectDirectiveFeatureBase : IDialectDirectiveFeature
{
    public abstract DialectDirectiveKind Kind { get; }

    public abstract string Keyword { get; }

    public string LexemeTag => $"DialectDirectiveKeyword.{Keyword}";

    public abstract DialectDirectiveArgumentShape ArgumentShape { get; }

    public abstract float ParserPriority { get; }

    public virtual bool IsSingleton => false;

    public virtual IAstNodeCreator CreateNodeCreator()
    {
        return ArgumentShape switch
        {
            DialectDirectiveArgumentShape.Identifier => new SingleIdentifierDialectDirectiveNodeCreator(this),
            _ => new IdentifierListDialectDirectiveNodeCreator(this)
        };
    }

    public virtual void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        Thrower.InvalidOpEx($"Dialect feature '{GetType().Name}' does not support line accumulation.");
        return;
    }

    public virtual void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
    }

    public abstract IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive);

    protected static IReadOnlyList<string> GetIdentifierList(DialectDirectiveAstNode directive)
    {
        if (directive is not IdentifierListDirectiveAstNode)
        {
            Thrower.Argument(nameof(directive), $"Directive '{directive.GetType().Name}' must provide an identifier list.");
        }

        return ((IdentifierListDirectiveAstNode)directive).Identifiers.Identifiers.Select(x => x.Identifier).ToList();
    }

    protected static string GetSingleIdentifier(DialectDirectiveAstNode directive)
    {
        if (directive is not SingleIdentifierDirectiveAstNode)
        {
            Thrower.Argument(nameof(directive), $"Directive '{directive.GetType().Name}' must provide one identifier.");
        }

        return ((SingleIdentifierDirectiveAstNode)directive).Identifier.Identifier;
    }
}

internal sealed class UseModulesDialectDirectiveFeature : DialectDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.UseModules;
    public override string Keyword => DialectDslKeywords.Use;
    public override DialectDirectiveArgumentShape ArgumentShape => DialectDirectiveArgumentShape.IdentifierList;
    public override float ParserPriority => 11f;

    public override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        accumulation.UseModules.AddRange(DialectDirectiveParserSupport.ParseIdentifierList(line, Keyword));
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.AddUseModules(GetIdentifierList(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) => [new UseModulesAirAnnotation(GetIdentifierList(directive))];
}

internal sealed class ExcludeModulesDialectDirectiveFeature : DialectDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.ExcludeModules;
    public override string Keyword => DialectDslKeywords.Exclude;
    public override DialectDirectiveArgumentShape ArgumentShape => DialectDirectiveArgumentShape.IdentifierList;
    public override float ParserPriority => 12f;

    public override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        accumulation.ExcludeModules.AddRange(DialectDirectiveParserSupport.ParseIdentifierList(line, Keyword));
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.AddExcludeModules(GetIdentifierList(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) => [new ExcludeModulesAirAnnotation(GetIdentifierList(directive))];
}

internal abstract class OrderDialectDirectiveFeatureBase : DialectDirectiveFeatureBase
{
    protected abstract DialectOrderDirectiveKind OrderKind { get; }

    protected abstract Action<DialectDirectiveValidationState, IReadOnlyList<string>, LexemeValue?> ValidationAction { get; }

    public override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        GetTargetList(accumulation).AddRange(DialectDirectiveParserSupport.ParseIdentifierList(line, Keyword));
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        ValidationAction(state, GetIdentifierList(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new OrderAirAnnotation(OrderKind, GetIdentifierList(directive))];
    }

    protected abstract List<string> GetTargetList(DialectDirectiveAccumulation accumulation);
}

internal sealed class RequiresModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.RequiresModules;
    public override string Keyword => DialectDslKeywords.Requires;
    public override DialectDirectiveArgumentShape ArgumentShape => DialectDirectiveArgumentShape.IdentifierList;
    public override float ParserPriority => 13f;
    protected override DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.Requires;
    protected override Action<DialectDirectiveValidationState, IReadOnlyList<string>, LexemeValue?> ValidationAction => static (state, values, token) => state.AddRequiresModules(values, token);
    protected override List<string> GetTargetList(DialectDirectiveAccumulation accumulation) => accumulation.RequiresModules;
}

internal sealed class BeforeModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.BeforeModules;
    public override string Keyword => DialectDslKeywords.Before;
    public override DialectDirectiveArgumentShape ArgumentShape => DialectDirectiveArgumentShape.IdentifierList;
    public override float ParserPriority => 14f;
    protected override DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.Before;
    protected override Action<DialectDirectiveValidationState, IReadOnlyList<string>, LexemeValue?> ValidationAction => static (state, values, token) => state.AddBeforeModules(values, token);
    protected override List<string> GetTargetList(DialectDirectiveAccumulation accumulation) => accumulation.BeforeModules;
}

internal sealed class AfterModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.AfterModules;
    public override string Keyword => DialectDslKeywords.After;
    public override DialectDirectiveArgumentShape ArgumentShape => DialectDirectiveArgumentShape.IdentifierList;
    public override float ParserPriority => 15f;
    protected override DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.After;
    protected override Action<DialectDirectiveValidationState, IReadOnlyList<string>, LexemeValue?> ValidationAction => static (state, values, token) => state.AddAfterModules(values, token);
    protected override List<string> GetTargetList(DialectDirectiveAccumulation accumulation) => accumulation.AfterModules;
}

internal sealed class BackendDialectDirectiveFeature : DialectDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.Backend;
    public override string Keyword => DialectDslKeywords.Backend;
    public override DialectDirectiveArgumentShape ArgumentShape => DialectDirectiveArgumentShape.IdentifierList;
    public override float ParserPriority => 16f;

    public override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        accumulation.Backends.AddRange(DialectDirectiveParserSupport.ParseIdentifierList(line, Keyword));
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.AddBackends(GetIdentifierList(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) => [new BackendAirAnnotation(GetIdentifierList(directive))];
}

internal abstract class ToggleDirectiveFeatureBase : DialectDirectiveFeatureBase
{
    public override DialectDirectiveArgumentShape ArgumentShape => DialectDirectiveArgumentShape.Identifier;

    public override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        AddAccumulatedValue(accumulation, DialectDirectiveParserSupport.ParseSingleIdentifier(line, Keyword));
    }

    protected abstract void AddAccumulatedValue(DialectDirectiveAccumulation accumulation, string value);
}

internal sealed class AllowIntrinsicDialectDirectiveFeature : ToggleDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.AllowIntrinsic;
    public override string Keyword => DialectDslKeywords.Allow;
    public override float ParserPriority => 17f;

    protected override void AddAccumulatedValue(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.AllowedIntrinsics.Add(value);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.AddAllowedIntrinsic(GetSingleIdentifier(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) => [new IntrinsicAirAnnotation(GetSingleIdentifier(directive), allowed: true)];
}

internal sealed class ForbidIntrinsicDialectDirectiveFeature : ToggleDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.ForbidIntrinsic;
    public override string Keyword => DialectDslKeywords.Forbid;
    public override float ParserPriority => 18f;

    protected override void AddAccumulatedValue(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.ForbiddenIntrinsics.Add(value);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.AddForbiddenIntrinsic(GetSingleIdentifier(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) => [new IntrinsicAirAnnotation(GetSingleIdentifier(directive), allowed: false)];
}

internal sealed class EnableIntrinsicDialectDirectiveFeature : ToggleDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.EnableIntrinsic;
    public override string Keyword => DialectDslKeywords.Enable;
    public override float ParserPriority => 19f;

    protected override void AddAccumulatedValue(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.EnabledIntrinsics.Add(value);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.AddEnabledOptimizer(GetSingleIdentifier(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) => [new OptimizerAirAnnotation(GetSingleIdentifier(directive), enabled: true)];
}

internal sealed class DisableIntrinsicDialectDirectiveFeature : ToggleDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.DisableIntrinsic;
    public override string Keyword => DialectDslKeywords.Disable;
    public override float ParserPriority => 20f;

    protected override void AddAccumulatedValue(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.DisabledIntrinsics.Add(value);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.AddDisabledOptimizer(GetSingleIdentifier(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) => [new OptimizerAirAnnotation(GetSingleIdentifier(directive), enabled: false)];
}

internal sealed class SecurityDialectDirectiveFeature : ToggleDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.Security;
    public override string Keyword => DialectDslKeywords.Security;
    public override float ParserPriority => 21f;
    public override bool IsSingleton => true;

    protected override void AddAccumulatedValue(DialectDirectiveAccumulation accumulation, string value)
    {
        if (accumulation.SecurityProfile != null)
        {
            DialectDefinitionSliceParseErrors.Fail("Security directive can only be declared once.", null);
        }

        accumulation.SecurityProfile = DialectAnnotationValueGuard.ParseSecurityProfile(value);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.MarkSecurity(directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new SecurityAirAnnotation(DialectAnnotationValueGuard.ParseSecurityProfile(GetSingleIdentifier(directive)))];
    }
}

internal sealed class CapabilityDialectDirectiveFeature : DialectDirectiveFeatureBase
{
    public override DialectDirectiveKind Kind => DialectDirectiveKind.Capability;
    public override string Keyword => DialectDslKeywords.Capability;
    public override DialectDirectiveArgumentShape ArgumentShape => DialectDirectiveArgumentShape.IdentifierList;
    public override float ParserPriority => 22f;

    public override void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        accumulation.Capabilities.AddRange(DialectDirectiveParserSupport.ParseIdentifierList(line, Keyword));
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationState state)
    {
        state.AddCapabilities(GetIdentifierList(directive), directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) => [new CapabilityAirAnnotation(GetIdentifierList(directive))];
}

internal sealed class UseExcludeConflictDocumentValidationRule : IDialectDocumentValidationRule
{
    public int Order => 0;

    public void Validate(DialectDocumentAstNode document, DialectDirectiveValidationState state)
    {
        foreach (var conflict in state.UseModules.Intersect(state.ExcludeModules, StringComparer.Ordinal))
        {
            DialectDefinitionSliceParseErrors.Fail($"Module '{conflict}' cannot appear in both use and exclude directives.", document.Declaration.NameNode.LexemeValue);
        }
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
