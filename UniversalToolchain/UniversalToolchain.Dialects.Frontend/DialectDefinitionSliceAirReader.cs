using IntermediateRepresentationAbstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDefinitionSliceAirReader
{
    public static DialectDefinitionSlice Read(IAbstractIR air)
    {
        if (air == null)
        {
            Thrower.ArgumentNull(nameof(air));
        }

        var slice = air.Instructions
            .SelectMany(x => x.Metadata)
            .OfType<DialectDefinitionSlice>()
            .SingleOrDefault();

        if (slice == null)
        {
            Thrower.InvalidOpEx("Dialect AIR did not contain a DialectDefinitionSlice annotation.");
        }

        return slice;
    }
}
