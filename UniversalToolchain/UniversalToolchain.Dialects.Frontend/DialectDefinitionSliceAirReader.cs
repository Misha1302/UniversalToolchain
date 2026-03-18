using IntermediateRepresentationAbstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDefinitionSliceAirReader
{
    public static IReadOnlyList<IDialectAirAnnotation> Read(IAbstractIR air)
    {
        if (air == null)
            Thrower.ArgumentNull(nameof(air));

        return air.Instructions
            .SelectMany(x => x.Metadata)
            .OfType<IDialectAirAnnotation>()
            .ToList();
    }
}
