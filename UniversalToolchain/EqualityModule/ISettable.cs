namespace EqualityModule;

public interface ISettable<in TValue>
{
    public void SetValue(TValue value);
}