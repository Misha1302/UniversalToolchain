// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;

namespace DynamicMethodWrapper;

public interface IDynamicMethodConvertable
{
    public string Name { get; }
    int ParamsCount { get; }

    public DynamicMethod ToDynamicMethod(Type? returnType, IList<Type> args);
}