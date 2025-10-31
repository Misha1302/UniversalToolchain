// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using EqualityModule;

namespace VariablesModule;

public class VariableReference<T>(Action<T> set) : ISettable<T>
{
    public void SetValue(T value)
    {
        set(value);
    }
}