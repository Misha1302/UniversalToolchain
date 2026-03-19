namespace DynamicMethodWrapper;

public class AbstractMethodImpl(
    string name,
    Action<IAbstractIR, IAbstractMethodConvertable.Context> bodyGenerator
) : IAbstractMethodConvertable
{
    public string Name => name;

    public IAbstractIR GetAbstractIR(IAbstractMethodConvertable.Context context)
    {
        var air = new AbstractIR();
        bodyGenerator(air, context);
        return air;
    }

    public override string ToString() => Name;
}