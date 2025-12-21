using EqualityModule;

namespace VariablesModule;

public class VariableReference<T>(Action<T> set) : ISettable<T>
{
    public void SetValue(T value)
    {
        set(value);
    }
}