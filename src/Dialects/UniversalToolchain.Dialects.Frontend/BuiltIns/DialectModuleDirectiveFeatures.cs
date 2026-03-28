namespace UniversalToolchain.Dialects.Frontend;

internal sealed class UseModulesDialectDirectiveFeature : IdentifierListDialectDirectiveFeatureBase
{
    public const string FeatureId = "builtin.modules.use";

    public override string Id => FeatureId;

    public override string Keyword => DialectDslKeywords.Use;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleSelection, 0);

    override protected void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        accumulation.UseModules.AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(DialectDirectiveValidationKeys.UseModules, GetIdentifierListArgument(directive), "Duplicate use module is not allowed.", directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) =>
        [new UseModulesAirAnnotation(GetIdentifierListArgument(directive))];
}

internal sealed class ExcludeModulesDialectDirectiveFeature : IdentifierListDialectDirectiveFeatureBase
{
    public const string FeatureId = "builtin.modules.exclude";

    public override string Id => FeatureId;

    public override string Keyword => DialectDslKeywords.Exclude;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleSelection, 1);

    override protected void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        accumulation.ExcludeModules.AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(DialectDirectiveValidationKeys.ExcludeModules, GetIdentifierListArgument(directive), "Duplicate exclude module is not allowed.", directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) =>
        [new ExcludeModulesAirAnnotation(GetIdentifierListArgument(directive))];
}

internal abstract class OrderDialectDirectiveFeatureBase : IdentifierListDialectDirectiveFeatureBase
{
    protected abstract DialectOrderDirectiveKind OrderKind { get; }

    protected abstract DialectSetStateKey<string> ValidationKey { get; }

    protected abstract string DuplicateMessage { get; }

    protected abstract List<string> GetAccumulationTarget(DialectDirectiveAccumulation accumulation);

    override protected void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        GetAccumulationTarget(accumulation).AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(ValidationKey, GetIdentifierListArgument(directive), DuplicateMessage, directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) =>
        [new OrderAirAnnotation(OrderKind, GetIdentifierListArgument(directive))];
}

internal sealed class RequiresModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    private static readonly DialectSetStateKey<string> ValidationStateKey = new("builtin.order.requires", StringComparer.Ordinal);

    public override string Id => "builtin.order.requires";

    public override string Keyword => DialectDslKeywords.Requires;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleOrdering, 0);

    override protected DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.Requires;

    override protected DialectSetStateKey<string> ValidationKey => ValidationStateKey;

    override protected string DuplicateMessage => "Duplicate requires module is not allowed.";

    override protected List<string> GetAccumulationTarget(DialectDirectiveAccumulation accumulation) => accumulation.RequiresModules;
}

internal sealed class BeforeModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    private static readonly DialectSetStateKey<string> ValidationStateKey = new("builtin.order.before", StringComparer.Ordinal);

    public override string Id => "builtin.order.before";

    public override string Keyword => DialectDslKeywords.Before;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleOrdering, 1);

    override protected DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.Before;

    override protected DialectSetStateKey<string> ValidationKey => ValidationStateKey;

    override protected string DuplicateMessage => "Duplicate before module is not allowed.";

    override protected List<string> GetAccumulationTarget(DialectDirectiveAccumulation accumulation) => accumulation.BeforeModules;
}

internal sealed class AfterModulesDialectDirectiveFeature : OrderDialectDirectiveFeatureBase
{
    private static readonly DialectSetStateKey<string> ValidationStateKey = new("builtin.order.after", StringComparer.Ordinal);

    public override string Id => "builtin.order.after";

    public override string Keyword => DialectDslKeywords.After;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.ModuleOrdering, 2);

    override protected DialectOrderDirectiveKind OrderKind => DialectOrderDirectiveKind.After;

    override protected DialectSetStateKey<string> ValidationKey => ValidationStateKey;

    override protected string DuplicateMessage => "Duplicate after module is not allowed.";

    override protected List<string> GetAccumulationTarget(DialectDirectiveAccumulation accumulation) => accumulation.AfterModules;
}

internal sealed class BackendDialectDirectiveFeature : IdentifierListDialectDirectiveFeatureBase
{
    private static readonly DialectSetStateKey<string> ValidationKey = new("builtin.backends.enable", StringComparer.Ordinal);

    public override string Id => "builtin.backends.enable";

    public override string Keyword => DialectDslKeywords.Backend;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.BackendSelection, 0);

    override protected void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        accumulation.Backends.AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(ValidationKey, GetIdentifierListArgument(directive), "Duplicate backend identifier is not allowed.", directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) =>
        [new BackendAirAnnotation(GetIdentifierListArgument(directive))];
}

internal sealed class CapabilityDialectDirectiveFeature : IdentifierListDialectDirectiveFeatureBase
{
    private static readonly DialectSetStateKey<string> ValidationKey = new("builtin.capabilities.enable", StringComparer.Ordinal);

    public override string Id => "builtin.capabilities.enable";

    public override string Keyword => DialectDslKeywords.Capability;

    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Capabilities, 0);

    override protected void AccumulateIdentifiers(DialectDirectiveAccumulation accumulation, IReadOnlyList<string> values)
    {
        accumulation.Capabilities.AddRange(values);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        context.AddValues(ValidationKey, GetIdentifierListArgument(directive), "Duplicate capability identifier is not allowed.", directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) =>
        [new CapabilityAirAnnotation(GetIdentifierListArgument(directive))];
}

internal sealed class UseExcludeConflictDocumentValidationRule : IDialectDocumentValidationRule
{
    public int Order => 0;

    public void Validate(DialectDocumentAstNode document, DialectDirectiveValidationContext context)
    {
        foreach (var conflict in context.GetValues(DialectDirectiveValidationKeys.UseModules).Intersect(context.GetValues(DialectDirectiveValidationKeys.ExcludeModules), StringComparer.Ordinal))
            DialectDefinitionSliceParseErrors.Fail($"Module '{conflict}' cannot appear in both use and exclude directives.", document.Declaration.NameNode.LexemeValue);
    }
}