using System.Reflection.Emit;
using GrEmit;

namespace DynamicMethodWrapper;

public interface IDynamicMethodConvertable
{
    public string Name { get; }
    int ParamsCount { get; }

    public (GroboIL, DynamicMethod) ToDynamicMethod(Context context);

    public record Context(IReadOnlyList<Type> Args, IReadOnlyList<Type> Stack);
}