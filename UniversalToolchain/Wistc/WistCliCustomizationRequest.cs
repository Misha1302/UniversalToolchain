namespace Wistc;

internal sealed record WistCliCustomizationRequest
{
    public bool HasCustomization => false;

    public static WistCliCustomizationRequest FromOptions(CommonOptions options)
    {
        options.ArgNotNull();
        return new WistCliCustomizationRequest();
    }
}
