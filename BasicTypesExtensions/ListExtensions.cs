// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace BasicTypesExtensions;

public static class ListExtensions
{
    public static (T item, int index) FirstStarts<T>(this List<T> list, Func<T, bool> predicate, int startIndex)
    {
        for (var i = startIndex; i < list.Count; i++)
            if (predicate(list[i]))
                return (list[i], i);
        return default!;
    }
}