namespace EqualityModule;

public interface IGettable<out TValue>
{
    public TValue GetValue();
}