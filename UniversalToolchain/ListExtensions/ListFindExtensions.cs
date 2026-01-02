namespace ListExtensions;

public static class ListFindExtensions
{
    public static (T? item, int index) FirstStarts<T>(this List<T> list, Func<T, bool> predicate, int startIndex)
    {
        for (var i = startIndex; i < list.Count; i++)
            if (predicate(list[i]))
                return (list[i], i);
        return default;
    }
}