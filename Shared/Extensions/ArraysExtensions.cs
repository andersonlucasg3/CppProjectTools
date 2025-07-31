namespace Shared.Extensions;

public static class ArraysExtensions
{
    public static void AddRange<TElement>(this List<TElement> List, params IReadOnlyList<TElement>[] Lists)
    {
        foreach (IReadOnlyList<TElement> Each in Lists)
        {
            List.AddRange(Each);
        }
    }
}