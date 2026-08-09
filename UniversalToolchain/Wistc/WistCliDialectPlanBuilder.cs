using UniversalToolchain.Wist.LanguagePack;

namespace Wistc;

internal sealed class WistCliDialectPlanBuilder
{
    public WistCliDialectPlan Build(CommonOptions options)
    {
        options.ArgNotNull();
        return WistCliDialectPlan.Preset(WistLanguageDefinitions.FullDefaultId);
    }
}
