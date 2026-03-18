using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

internal abstract class IntrinsicPolicyDialectDirectiveFeatureBase : SingleIdentifierDialectDirectiveFeatureBase
{
    protected abstract bool Allowed { get; }

    protected abstract string DuplicateMessage { get; }

    protected abstract string ContradictionMessageTemplate { get; }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        var value = GetSingleIdentifierArgument(directive);
        var state = context.GetOrAddState(DialectDirectiveValidationKeys.IntrinsicToggle, static () => new ToggleValidationState(StringComparer.Ordinal));
        state.Add(value, Allowed, DuplicateMessage, ContradictionMessageTemplate, directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new IntrinsicAirAnnotation(GetSingleIdentifierArgument(directive), Allowed)];
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
    protected abstract bool Enabled { get; }

    protected abstract string DuplicateMessage { get; }

    protected abstract string ContradictionMessageTemplate { get; }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        var value = GetSingleIdentifierArgument(directive);
        var state = context.GetOrAddState(DialectDirectiveValidationKeys.OptimizerToggle, static () => new ToggleValidationState(StringComparer.Ordinal));
        state.Add(value, Enabled, DuplicateMessage, ContradictionMessageTemplate, directive.LexemeValue);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new OptimizerAirAnnotation(GetSingleIdentifierArgument(directive), Enabled)];
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

    public override string SingletonViolationMessage => "Security directive can only be declared once.";

    protected override void AccumulateIdentifier(DialectDirectiveAccumulation accumulation, string value)
    {
        accumulation.SetSingletonValue(DialectDirectiveAccumulation.Keys.SecurityProfile, DialectAnnotationValueGuard.ParseSecurityProfile(value), SingletonViolationMessage);
    }

    public override void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        GetSingleIdentifierArgument(directive);
    }

    public override IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        return [new SecurityAirAnnotation(DialectAnnotationValueGuard.ParseSecurityProfile(GetSingleIdentifierArgument(directive)))];
    }
}
