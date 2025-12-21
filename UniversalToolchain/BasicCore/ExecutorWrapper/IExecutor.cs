using System.Reflection.Emit;
using GrEmit;

namespace BasicCore.ExecutorWrapper;

public interface IExecutor
{
    object Execute(List<(GroboIL, DynamicMethod)> methods);
}