namespace BasicCore;

public interface IExecutableGiver<out T>
{
    public T GetExecutable(string code, Dictionary<string, Type> parameters);
}