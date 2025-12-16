// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace ConditionsModule;

public static class Comparisons
{
    public static bool Equal<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b) == 0;
    }

    public static bool NotEqual<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b) != 0;
    }

    public static bool Greater<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b) > 0;
    }

    public static bool Less<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b) < 0;
    }

    public static bool GreaterOrEqual<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b) >= 0;
    }

    public static bool LessOrEqual<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b) <= 0;
    }
}