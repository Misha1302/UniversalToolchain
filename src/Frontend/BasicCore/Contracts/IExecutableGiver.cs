namespace BasicCore.Contracts;

public interface IExecutableGiver<out T>
{
    public T GetExecutable(string code, OrderedDictionary<string, Type>? parameters = null);
}