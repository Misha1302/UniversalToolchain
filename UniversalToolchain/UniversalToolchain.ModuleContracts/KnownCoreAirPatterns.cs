namespace UniversalToolchain.ModuleContracts;

public static class KnownCoreAirPatterns
{
    public static AirPatternId UniversalCall { get; } = new("core.air.intrinsic.call-csharp");

    public static AirPatternId UniversalConstructorCall { get; } = new("core.air.intrinsic.call-csharp-ctor");

    public static AirPatternId Label { get; } = new("core.air.label");

    public static AirPatternId Jump { get; } = new("core.air.jump");
}
