using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace DynamicMethodWrapper;

public class AbstractMethodImpl(
    string name,
    int argsCount,
    Action<IAbstractIR, IAbstractMethodConvertable.Context> bodyGenerator,
    Func<IAbstractMethodConvertable.Context, Type> returnType
) : IAbstractMethodConvertable
{
    public string Name => name;
    public int ParamsCount => argsCount;

    public Type GetReturnType(IAbstractMethodConvertable.Context context)
    {
        return returnType(context).NotNull();
    }

    public IAbstractIR GetAbstractIR(IAbstractMethodConvertable.Context context)
    {
        var air = new AbstractIR();
        bodyGenerator(air, context);
        return air;
    }

    public override string ToString()
    {
        return Name;
    }
}