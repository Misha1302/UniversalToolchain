namespace ObjectExtensions;

public static class ObjectExtension
{
    public static T Get<T>(this object obj) => (T)obj;

    // ReSharper disable once ReturnTypeCanBeNotNullable
    public static T? MakeNullable<T>(this T obj) => obj;
}
