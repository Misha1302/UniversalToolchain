namespace ObjectExtensions;

public static class ObjectExtension
{
    public static T Get<T>(this object obj)
    {
        return (T)obj;
    }
}