using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceParser
{
    public DialectDefinitionSlice Parse(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        var document = DialectDslAstValidator.Validate(astRoot);
        var annotations = DialectAstLowering.Lower(document);
        var aggregation = new DialectDefinitionAggregation();
        foreach (var annotation in annotations)
        {
            switch (annotation)
            {
                case DialectNameAirAnnotation dialectName:
                    aggregation.SetDialectName(dialectName.Name);
                    break;
                case UseModulesAirAnnotation useModules:
                    aggregation.AddUseModules(useModules.Modules);
                    break;
                case ExcludeModulesAirAnnotation excludeModules:
                    aggregation.AddExcludeModules(excludeModules.Modules);
                    break;
                case RequiresModulesAirAnnotation requiresModules:
                    aggregation.AddRequiresModules(requiresModules.Modules);
                    break;
                case BeforeModulesAirAnnotation beforeModules:
                    aggregation.AddBeforeModules(beforeModules.Modules);
                    break;
                case AfterModulesAirAnnotation afterModules:
                    aggregation.AddAfterModules(afterModules.Modules);
                    break;
                case BackendAirAnnotation backend:
                    aggregation.AddBackends(backend.Backends);
                    break;
                case AllowIntrinsicAirAnnotation allowIntrinsic:
                    aggregation.AddAllowedIntrinsic(allowIntrinsic.IntrinsicName);
                    break;
                case ForbidIntrinsicAirAnnotation forbidIntrinsic:
                    aggregation.AddForbiddenIntrinsic(forbidIntrinsic.IntrinsicName);
                    break;
                case EnableIntrinsicAirAnnotation enableIntrinsic:
                    aggregation.AddEnabledIntrinsic(enableIntrinsic.IntrinsicName);
                    break;
                case DisableIntrinsicAirAnnotation disableIntrinsic:
                    aggregation.AddDisabledIntrinsic(disableIntrinsic.IntrinsicName);
                    break;
                case SecurityAirAnnotation security:
                    aggregation.SetSecurityProfile(security.Profile);
                    break;
                case CapabilityAirAnnotation capability:
                    aggregation.AddCapabilities(capability.Capabilities);
                    break;
                default:
                    Thrower.InvalidOpEx($"Dialect lowering produced unsupported annotation type '{annotation.GetType().FullName}'.");
                    break;
            }
        }

        return new DialectDefinitionSliceBuilder().Build(aggregation);
    }
}
