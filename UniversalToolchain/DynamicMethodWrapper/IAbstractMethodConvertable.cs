using IntermediateRepresentationAbstractions;

namespace DynamicMethodWrapper;

public interface IAbstractMethodConvertable
{
    public string Name { get; }
    int ParamsCount { get; }

    public Type GetReturnType(Context context);

    // ReSharper disable once InconsistentNaming
    public IAbstractIR GetAbstractIR(Context context);

    public record Context(IReadOnlyList<Type> Stack);
}