namespace ObjectExtensions;

public static class ObjectExtension
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly List<object> _immortalObjects = [];

    public static T Get<T>(this object obj) => (T)obj;

    public static void MakeImmortal(this object obj)
    {
        lock (_immortalObjects)
        {
            _immortalObjects.Add(obj);
        }
    }

    public static void MakeMortal(this object obj)
    {
        lock (_immortalObjects)
        {
            _immortalObjects.Remove(obj);
        }
    }
}