namespace SettableGettableModule;

public class VariableReference<T>(Action<T> set) : ISettable<T>
{
    public void SetValue(T value)
    {
        set(value);
    }
}