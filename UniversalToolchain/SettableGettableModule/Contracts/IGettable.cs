namespace SettableGettableModule.Contracts;

public interface IGettable<out TValue>
{
    public TValue GetValue();
}