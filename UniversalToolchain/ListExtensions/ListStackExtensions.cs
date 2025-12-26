namespace ListExtensions;

public static class ListStackExtensions
{
    public static void Push<T>(this List<T> list, T value)
    {
        list.Add(value);
    }

    public static T Pop<T>(this List<T> list)
    {
        var value = list[^1];
        list.RemoveAt(list.Count - 1);
        return value;
    }
}