using System.Collections.Generic;

namespace Common.EnumerableExtensions;

static class EnumerableExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
    {
        foreach (var it in source)
        {
            if (it is { } val)
                yield return val;
        }
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : struct
    {
        foreach (var it in source)
        {
            if (it is { } val)
                yield return val;
        }
    }
}
