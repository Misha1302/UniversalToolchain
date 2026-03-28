namespace DynamicMethodWrapper;

public interface IAbstractMethodConvertable
{
    public string Name { get; }

    // ReSharper disable once InconsistentNaming
    public IAbstractIR GetAbstractIR(Context context);

    public record Context(IReadOnlyList<Type> Stack);
}