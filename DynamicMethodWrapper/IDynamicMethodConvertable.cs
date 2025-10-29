// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using GrEmit;

namespace DynamicMethodWrapper;

public interface IDynamicMethodConvertable
{
    public string Name { get; }
    int ParamsCount { get; }

    public (GroboIL, DynamicMethod) ToDynamicMethod(Type? preferedReturnType, IList<Type> args);
}