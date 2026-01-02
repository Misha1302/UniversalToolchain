namespace ObjectExtensions;

public static class ObjectExtension
{
    public static T Get<T>(this object obj) => (T)obj;
}